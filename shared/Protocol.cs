using System.Text;
using System.Text.Json;

namespace Shared;

public enum MsgType { Join, Chat, System, History }

public enum ChatTarget { All, InstructorOnly, Specific }

/// <summary>강사/수강생 이름 관련 예약어.</summary>
public static class Names
{
    public const string Instructor = "강사";
}

/// <summary>채팅 TCP 연결 위에서 주고받는 한 줄(JSON) 메시지 단위.</summary>
public class ChatEnvelope
{
    public MsgType Type { get; set; }
    public string From { get; set; } = "";
    public ChatTarget Target { get; set; } = ChatTarget.All;
    /// <summary>Target == Specific 일 때만 사용: 강사가 지정한 수신 대상 수강생 이름.</summary>
    public string? To { get; set; }
    public string Text { get; set; } = "";
    public DateTime Time { get; set; } = DateTime.Now;
    /// <summary>있으면 이미지 메시지(PNG 바이트). JSON 상에서는 자동으로 base64 문자열로 직렬화된다.</summary>
    public byte[]? Image { get; set; }

    /// <summary>Type == History 일 때만 사용: 접속 시점까지의 강사 공지 이력.</summary>
    public List<ChatEnvelope>? History { get; set; }
}

/// <summary>
/// 줄바꿈으로 구분된 JSON 메시지를 스트림에 쓰고 읽는다. BOM 없는 UTF-8 고정.
/// 각 줄은 AES-256-GCM으로 암호화된 base64 블롭이라 네트워크를 스니핑해도 평문이 보이지 않는다.
/// </summary>
public static class LineProtocol
{
    public static readonly Encoding NoBom = new UTF8Encoding(false);

    public static async Task SendAsync(StreamWriter writer, ChatEnvelope env, byte[] key)
    {
        var json = JsonSerializer.Serialize(env);
        var encrypted = CryptoUtil.Encrypt(key, json);
        await writer.WriteLineAsync(encrypted);
        await writer.FlushAsync();
    }

    public static async Task<ChatEnvelope?> ReceiveAsync(StreamReader reader, byte[] key)
    {
        var line = await reader.ReadLineAsync();
        if (line == null) return null;
        var json = CryptoUtil.Decrypt(key, line);
        return JsonSerializer.Deserialize<ChatEnvelope>(json);
    }
}
