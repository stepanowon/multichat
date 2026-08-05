using System.Text.Json;

namespace Shared;

/// <summary>
/// 채팅 메시지를 로컬 파일(%AppData%\MultiChat\...)에 한 줄당 JSON 하나씩 append로 저장하고,
/// 다음 실행 때 그대로 불러온다. 서버/클라이언트가 각자 자신이 본 메시지를 저장하므로 파일을 분리한다.
/// </summary>
public class ChatHistoryStore
{
    private readonly string _path;

    public ChatHistoryStore(string fileName)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MultiChat");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, fileName);
    }

    public List<ChatEnvelope> Load()
    {
        var result = new List<ChatEnvelope>();
        if (!File.Exists(_path)) return result;

        foreach (var line in File.ReadLines(_path, LineProtocol.NoBom))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var env = JsonSerializer.Deserialize<ChatEnvelope>(line);
                if (env != null) result.Add(env);
            }
            catch { /* 손상된 줄은 건너뛴다 */ }
        }
        return result;
    }

    // ponytail: 파일이 무한정 커질 수 있음 - 강의실 단기 사용 기준으로는 문제 없음. 필요해지면 날짜별 로테이션 추가.
    public void Append(ChatEnvelope env)
    {
        try
        {
            File.AppendAllText(_path, JsonSerializer.Serialize(env) + "\n", LineProtocol.NoBom);
        }
        catch { /* 저장 실패해도 채팅 자체는 계속되어야 함 */ }
    }
}
