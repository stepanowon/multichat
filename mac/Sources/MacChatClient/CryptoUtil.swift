import Foundation
import CryptoKit

/// shared/CryptoUtil.cs와 대응. AES-256-GCM, combined = nonce(12) + cipher + tag(16), base64.
/// CryptoKit의 AES.GCM.SealedBox.combined이 기본 12바이트 nonce에서 정확히 이 포맷이라 그대로 호환된다.
enum CryptoUtil {
    static let keySizeBytes = 32

    static func encrypt(key: SymmetricKey, plaintext: String) throws -> String {
        let sealed = try AES.GCM.seal(Data(plaintext.utf8), using: key)
        guard let combined = sealed.combined else { throw ChatError.protocolError("암호화 실패") }
        return combined.base64EncodedString()
    }

    static func decrypt(key: SymmetricKey, base64: String) throws -> String {
        guard let data = Data(base64Encoded: base64) else { throw ChatError.protocolError("잘못된 데이터") }
        let sealed = try AES.GCM.SealedBox(combined: data)
        let plain = try AES.GCM.open(sealed, using: key)
        guard let s = String(data: plain, encoding: .utf8) else { throw ChatError.protocolError("복호화 실패") }
        return s
    }
}
