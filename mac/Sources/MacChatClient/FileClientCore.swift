import Foundation

/// client/FileClientCore.cs와 대응. 서버의 파일 목록/다운로드/키배포 TCP 포트를 사용. 요청마다 새 연결.
final class FileClientCore {
    private let host: String
    private let port: Int

    init(host: String, port: Int) {
        self.host = host
        self.port = port
    }

    func list() throws -> [FileEntry] {
        let s = try TCPStream(host: host, port: port)
        defer { s.close() }
        try s.writeLine("LIST")

        var result: [FileEntry] = []
        while let line = try s.readLine(), line != "END" {
            let parts = line.split(separator: "|", maxSplits: 1)
            if parts.count == 2, let size = Int64(parts[1]) {
                result.append(FileEntry(name: String(parts[0]), size: size))
            }
        }
        return result
    }

    func download(fileName: String, to url: URL) throws {
        let s = try TCPStream(host: host, port: port)
        defer { s.close() }
        try s.writeLine("GET|\(fileName)")

        guard let status = try s.readLine(), status == "OK" else {
            throw ChatError.protocolError("파일을 가져오지 못했습니다.")
        }
        let lenData = try s.readExact(8)
        let len = lenData.withUnsafeBytes { $0.load(as: Int64.self) }

        FileManager.default.createFile(atPath: url.path, contents: nil)
        let handle = try FileHandle(forWritingTo: url)
        defer { try? handle.close() }

        var remaining = len
        while remaining > 0 {
            let chunkSize = Int(min(remaining, 81920))
            handle.write(try s.readExact(chunkSize))
            remaining -= Int64(chunkSize)
        }
    }

    /// 서버 세션의 채팅 암호화 키를 받아온다.
    func getKey() throws -> Data {
        let s = try TCPStream(host: host, port: port)
        defer { s.close() }
        try s.writeLine("GETKEY")

        guard let status = try s.readLine(), status == "OK" else {
            throw ChatError.protocolError("암호화 키를 가져오지 못했습니다.")
        }
        return try s.readExact(CryptoUtil.keySizeBytes)
    }
}
