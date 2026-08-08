import Foundation
import Darwin

/// 서버 TCP 연결 하나를 감싼 동기식(블로킹) 줄 단위 read/write 래퍼.
/// POSIX 소켓을 직접 쓴다(Foundation Stream 대신) - Stream은 런루프에 스케줄하지 않고 쓰면
/// 네트워크가 잠깐 끊기거나 지연될 때 read()가 진짜 EOF가 아닌데도 스퓨리어스하게 0을 반환하는
/// 경우가 있어(대용량 전송 + 불안정한 Wi-Fi에서 실제로 재현됨), 의미가 명확한 POSIX read/write로
/// 대체했다. POSIX에서는 read()==0이 곧 상대가 정상 종료했다는 뜻이라 모호함이 없다.
final class TCPStream {
    private let fd: Int32
    private var readBuffer = Data()

    init(host: String, port: Int) throws {
        var hints = addrinfo(ai_flags: 0, ai_family: AF_UNSPEC, ai_socktype: SOCK_STREAM,
                              ai_protocol: IPPROTO_TCP, ai_addrlen: 0, ai_canonname: nil, ai_addr: nil, ai_next: nil)
        var resultPtr: UnsafeMutablePointer<addrinfo>?
        guard getaddrinfo(host, String(port), &hints, &resultPtr) == 0, let firstInfo = resultPtr else {
            throw ChatError.connectionFailed
        }
        defer { freeaddrinfo(resultPtr) }

        var connectedFd: Int32 = -1
        var info: UnsafeMutablePointer<addrinfo>? = firstInfo
        while let cur = info {
            let s = socket(cur.pointee.ai_family, cur.pointee.ai_socktype, cur.pointee.ai_protocol)
            if s >= 0 {
                if connect(s, cur.pointee.ai_addr, cur.pointee.ai_addrlen) == 0 {
                    connectedFd = s
                    break
                }
                Darwin.close(s)
            }
            info = cur.pointee.ai_next
        }
        guard connectedFd >= 0 else { throw ChatError.connectionFailed }
        fd = connectedFd

        // 연결이 끊긴 소켓에 쓸 때 SIGPIPE로 프로세스가 죽는 것을 방지 (에러 반환으로만 처리)
        var noSigPipe: Int32 = 1
        setsockopt(fd, SOL_SOCKET, SO_NOSIGPIPE, &noSigPipe, socklen_t(MemoryLayout<Int32>.size))
    }

    func writeLine(_ s: String) throws {
        try write(Data((s + "\n").utf8))
    }

    func write(_ data: Data) throws {
        try data.withUnsafeBytes { (raw: UnsafeRawBufferPointer) in
            var offset = 0
            while offset < raw.count {
                let n = Darwin.write(fd, raw.baseAddress!.advanced(by: offset), raw.count - offset)
                if n > 0 { offset += n; continue }
                if n < 0, errno == EINTR { continue }
                throw ChatError.connectionClosed
            }
        }
    }

    /// '\n' 기준 한 줄을 읽는다(끝의 '\r'은 제거). 연결이 정상 종료되면 nil.
    func readLine() throws -> String? {
        while true {
            if let idx = readBuffer.firstIndex(of: 0x0A) {
                var lineData = readBuffer.subdata(in: readBuffer.startIndex..<idx)
                readBuffer.removeSubrange(readBuffer.startIndex...idx)
                if lineData.last == 0x0D { lineData.removeLast() }
                return String(data: lineData, encoding: .utf8)
            }
            guard let chunk = try readSome() else { return nil }
            readBuffer.append(chunk)
        }
    }

    func readExact(_ count: Int) throws -> Data {
        while readBuffer.count < count {
            guard let chunk = try readSome() else { throw ChatError.connectionClosed }
            readBuffer.append(chunk)
        }
        let end = readBuffer.index(readBuffer.startIndex, offsetBy: count)
        let result = readBuffer.subdata(in: readBuffer.startIndex..<end)
        readBuffer.removeSubrange(readBuffer.startIndex..<end)
        return result
    }

    /// 소켓에서 최대 64KB를 읽는다. 상대가 정상 종료하면 nil(POSIX read()==0, 모호함 없음).
    private func readSome() throws -> Data? {
        var buf = [UInt8](repeating: 0, count: 65536)
        while true {
            let n = buf.withUnsafeMutableBytes { ptr in
                Darwin.read(fd, ptr.baseAddress, ptr.count)
            }
            if n > 0 { return Data(buf[0..<n]) }
            if n == 0 { return nil }
            if errno == EINTR { continue }
            throw ChatError.connectionClosed
        }
    }

    /// shutdown()으로 다른 스레드에서 블로킹 중인 read를 안전하게 풀어준 뒤 닫는다
    /// (연결 종료 버튼 등 다른 스레드에서 호출될 수 있음).
    func close() {
        Darwin.shutdown(fd, SHUT_RDWR)
        Darwin.close(fd)
    }
}
