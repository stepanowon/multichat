import Foundation
import CryptoKit

/// client/ChatClientCore.cs와 대응. 채팅 TCP 연결 하나 + 백그라운드 수신 루프.
/// 콜백은 모두 메인 스레드에서 호출된다.
final class ChatClientCore {
    private var stream: TCPStream?
    private var key = SymmetricKey(data: Data(repeating: 0, count: CryptoUtil.keySizeBytes))

    var onMessage: ((ChatEnvelope) -> Void)?
    var onHistory: (([ChatEnvelope]) -> Void)?
    var onDisconnected: (() -> Void)?

    /// key는 미리 파일 포트의 GETKEY로 받아온 서버 세션 암호화 키.
    func connect(host: String, chatPort: Int, name: String, key: Data) throws {
        self.key = SymmetricKey(data: key)
        let s = try TCPStream(host: host, port: chatPort)
        stream = s
        try LineProtocol.send(s, env: ChatEnvelope(type: .join, from: name), key: self.key)
        Thread.detachNewThread { [weak self] in self?.receiveLoop() }
    }

    private func receiveLoop() {
        guard let s = stream else { return }
        do {
            while true {
                guard let env = try LineProtocol.receive(s, key: key) else { break }
                if env.type == .history, let hist = env.history {
                    let cb = onHistory
                    DispatchQueue.main.async { cb?(hist) }
                } else {
                    let cb = onMessage
                    DispatchQueue.main.async { cb?(env) }
                }
            }
        } catch {
            // 연결 끊김
        }
        let cb = onDisconnected
        DispatchQueue.main.async { cb?() }
    }

    func send(text: String, target: ChatTarget) {
        guard let s = stream else { return }
        try? LineProtocol.send(s, env: ChatEnvelope(type: .chat, target: target, text: text), key: key)
    }

    func sendImage(png: Data, target: ChatTarget) {
        guard let s = stream else { return }
        try? LineProtocol.send(s, env: ChatEnvelope(type: .chat, target: target, image: png), key: key)
    }

    /// 사용자가 직접 연결을 끊는다. 소켓을 닫으면 수신 루프의 블로킹 read가 풀려나오면서
    /// 자연스럽게 onDisconnected가 호출된다.
    func disconnect() {
        stream?.close()
        stream = nil
    }
}
