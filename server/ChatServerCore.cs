using System.Net;
using System.Net.Sockets;
using Shared;

namespace ChatServer;

internal class ConnectedClient
{
    public required TcpClient TcpClient { get; init; }
    public required StreamWriter Writer { get; init; }
    public required StreamReader Reader { get; init; }
    public string Name { get; set; } = "";
}

/// <summary>채팅 TCP 서버. 강사(서버 UI)와 다수 수강생 클라이언트 간 메시지 라우팅을 담당.</summary>
public class ChatServerCore
{
    private TcpListener? _listener;
    private byte[] _key = Array.Empty<byte>();
    private readonly List<ConnectedClient> _clients = new();
    // ponytail: 인메모리 이력. 서버 재시작 시 소실됨 - 필요해지면 파일로 영속화.
    private readonly List<ChatEnvelope> _instructorHistory = new();
    private readonly object _lock = new();

    /// <summary>학생 메시지가 서버(강사) 화면에 도착했을 때.</summary>
    public event Action<ChatEnvelope>? MessageArrived;
    /// <summary>접속자 목록이 바뀌었을 때(값은 의미 없음, 신호용).</summary>
    public event Action<string>? ClientListChanged;
    public event Action<string>? Log;

    /// <summary>이 세션에서 모든 채팅 메시지를 암호화할 키. 서버 시작 시 한 번 생성해서 전달한다.</summary>
    public async void Start(int port, byte[] key)
    {
        _key = key;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Log?.Invoke($"채팅 서버 시작: 포트 {port}");
        while (true)
        {
            TcpClient tcp;
            try { tcp = await _listener.AcceptTcpClientAsync(); }
            catch { break; }
            _ = HandleClientAsync(tcp);
        }
    }

    public void Stop() => _listener?.Stop();

    private async Task HandleClientAsync(TcpClient tcp)
    {
        var stream = tcp.GetStream();
        var reader = new StreamReader(stream, LineProtocol.NoBom);
        var writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = false };
        var client = new ConnectedClient { TcpClient = tcp, Writer = writer, Reader = reader };

        try
        {
            var join = await LineProtocol.ReceiveAsync(reader, _key);
            if (join == null || join.Type != MsgType.Join || string.IsNullOrWhiteSpace(join.From))
            {
                tcp.Close();
                return;
            }
            var name = join.From.Trim();
            if (name == Names.Instructor)
            {
                tcp.Close();
                return;
            }
            client.Name = name;

            lock (_lock) _clients.Add(client);
            ClientListChanged?.Invoke(client.Name);

            List<ChatEnvelope> historySnapshot;
            lock (_lock) historySnapshot = new List<ChatEnvelope>(_instructorHistory);
            await LineProtocol.SendAsync(writer, new ChatEnvelope { Type = MsgType.History, History = historySnapshot }, _key);

            // 접속/퇴장 시스템 메시지는 채팅창에 브로드캐스트하지 않음(자기 창에만 표시)

            while (true)
            {
                var env = await LineProtocol.ReceiveAsync(reader, _key);
                if (env == null) break;
                if (env.Type != MsgType.Chat) continue;

                env.From = client.Name;
                env.Time = DateTime.Now;

                MessageArrived?.Invoke(env);

                if (env.Target == ChatTarget.All)
                    await BroadcastAsync(env, exclude: client);
                // InstructorOnly: 강사 화면에만 표시되고 다른 클라이언트로는 전달하지 않음
            }
        }
        catch
        {
            // 연결 끊김 등은 무시하고 정리 단계로 진행
        }
        finally
        {
            lock (_lock) _clients.Remove(client);
            tcp.Close();
            if (!string.IsNullOrEmpty(client.Name))
            {
                ClientListChanged?.Invoke(client.Name);
            }
        }
    }

    /// <summary>
    /// 강사가 보내는 메시지. targetStudent가 null이면 전체 브로드캐스트(이력에 남아 늦게 접속하는
    /// 클라이언트에게도 전달됨), 이름이 지정되면 해당 수강생에게만 전달한다(이력에는 남기지 않음).
    /// 지정한 수강생이 접속 중이 아니면 false를 반환한다.
    /// </summary>
    public Task<bool> SendInstructorMessageAsync(string text, string? targetStudent) =>
        DispatchInstructorEnvelopeAsync(BuildInstructorEnvelope(targetStudent, text: text, image: null), targetStudent);

    /// <summary>강사가 보내는 이미지 메시지. 대상 규칙은 SendInstructorMessageAsync와 동일.</summary>
    public Task<bool> SendInstructorImageAsync(byte[] pngBytes, string? targetStudent) =>
        DispatchInstructorEnvelopeAsync(BuildInstructorEnvelope(targetStudent, text: "", image: pngBytes), targetStudent);

    private static ChatEnvelope BuildInstructorEnvelope(string? targetStudent, string text, byte[]? image) => new()
    {
        Type = MsgType.Chat,
        From = Names.Instructor,
        Target = targetStudent == null ? ChatTarget.All : ChatTarget.Specific,
        To = targetStudent,
        Text = text,
        Image = image,
        Time = DateTime.Now
    };

    private async Task<bool> DispatchInstructorEnvelopeAsync(ChatEnvelope env, string? targetStudent)
    {
        if (targetStudent == null)
        {
            lock (_lock) _instructorHistory.Add(env);
            await BroadcastAsync(env, exclude: null);
            return true;
        }

        ConnectedClient? target;
        lock (_lock) target = _clients.FirstOrDefault(c => c.Name == targetStudent);
        if (target == null) return false;

        try { await LineProtocol.SendAsync(target.Writer, env, _key); }
        catch { return false; }
        return true;
    }

    private async Task BroadcastAsync(ChatEnvelope env, ConnectedClient? exclude)
    {
        List<ConnectedClient> targets;
        lock (_lock) targets = _clients.Where(c => c != exclude).ToList();
        foreach (var c in targets)
        {
            try { await LineProtocol.SendAsync(c.Writer, env, _key); }
            catch { /* 해당 클라이언트는 자신의 읽기 루프에서 정리됨 */ }
        }
    }

    public List<string> GetClientNames()
    {
        lock (_lock) return _clients.Select(c => c.Name).ToList();
    }
}
