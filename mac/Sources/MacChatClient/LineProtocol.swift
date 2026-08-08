import Foundation
import CryptoKit

/// shared/Protocol.cs의 LineProtocol과 대응: 줄마다 AES-256-GCM으로 암호화된 base64 JSON.
enum LineProtocol {
    static func send(_ stream: TCPStream, env: ChatEnvelope, key: SymmetricKey) throws {
        let data = try JSONEncoder().encode(env)
        guard let json = String(data: data, encoding: .utf8) else { throw ChatError.protocolError("인코딩 실패") }
        let encrypted = try CryptoUtil.encrypt(key: key, plaintext: json)
        try stream.writeLine(encrypted)
    }

    static func receive(_ stream: TCPStream, key: SymmetricKey) throws -> ChatEnvelope? {
        guard let line = try stream.readLine() else { return nil }
        let json = try CryptoUtil.decrypt(key: key, base64: line)
        return try JSONDecoder().decode(ChatEnvelope.self, from: Data(json.utf8))
    }
}
