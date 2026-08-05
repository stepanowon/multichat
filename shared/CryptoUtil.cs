using System.Security.Cryptography;
using System.Text;

namespace Shared;

/// <summary>
/// AES-256-GCM 기반 대칭키 암호화. 서버가 세션마다 키를 새로 생성해 파일 포트로 배포하고,
/// 채팅 TCP 연결의 모든 줄(JSON)은 이 키로 암호화된 base64 블롭으로 오간다.
/// ponytail: 키 교환 자체는 평문 TCP라 능동적 중간자 공격까지는 못 막는다(TLS/PKI 아님) -
/// 목표는 "네트워크 스니핑으로 메시지 평문이 그대로 보이는 것"을 막는 것. 더 강한 보장이
/// 필요해지면 SslStream/인증서 기반으로 교체.
/// </summary>
public static class CryptoUtil
{
    public const int KeySizeBytes = 32; // AES-256
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public static byte[] GenerateKey()
    {
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public static string Encrypt(byte[] key, string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using (var aes = new AesGcm(key, TagSizeBytes))
            aes.Encrypt(nonce, plainBytes, cipher, tag);

        var combined = new byte[NonceSizeBytes + cipher.Length + TagSizeBytes];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSizeBytes);
        Buffer.BlockCopy(cipher, 0, combined, NonceSizeBytes, cipher.Length);
        Buffer.BlockCopy(tag, 0, combined, NonceSizeBytes + cipher.Length, TagSizeBytes);
        return Convert.ToBase64String(combined);
    }

    public static string Decrypt(byte[] key, string base64)
    {
        var combined = Convert.FromBase64String(base64);
        var nonce = combined[..NonceSizeBytes];
        var tag = combined[^TagSizeBytes..];
        var cipher = combined[NonceSizeBytes..^TagSizeBytes];
        var plain = new byte[cipher.Length];

        using (var aes = new AesGcm(key, TagSizeBytes))
            aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
