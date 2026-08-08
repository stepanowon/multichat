using System.Net.Sockets;
using Shared;

namespace ChatClient;

/// <summary>서버와의 채팅 TCP 연결 하나를 관리한다. 연결 유지 + 백그라운드 수신 루프.</summary>
public class ChatClientCore
{
    private TcpClient? _tcp;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private byte[] _key = Array.Empty<byte>();
    // ponytail: 재연결 직후 옛 수신 루프가 뒤늦게 종료되며 새 연결을 끊긴 걸로 오인하지 않도록
    // 연결마다 세대 번호를 매겨 자기 세대가 최신일 때만 Disconnected를 울린다.
    private int _generation;

    public event Action<ChatEnvelope>? MessageReceived;
    public event Action<List<ChatEnvelope>>? HistoryReceived;
    public event Action? Disconnected;

    public bool IsConnected => _tcp?.Connected == true;

    /// <summary>key는 미리 파일 포트의 GETKEY로 받아온 서버 세션 암호화 키.</summary>
    public async Task ConnectAsync(string host, int port, string name, byte[] key)
    {
        var myGeneration = ++_generation;
        _key = key;
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port);
        var stream = _tcp.GetStream();
        _reader = new StreamReader(stream, LineProtocol.NoBom);
        _writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = false };

        await LineProtocol.SendAsync(_writer, new ChatEnvelope { Type = MsgType.Join, From = name }, _key);

        _ = ReceiveLoopAsync(myGeneration);
    }

    private async Task ReceiveLoopAsync(int myGeneration)
    {
        try
        {
            while (true)
            {
                var env = await LineProtocol.ReceiveAsync(_reader!, _key);
                if (env == null) break;

                if (env.Type == MsgType.History && env.History != null)
                    HistoryReceived?.Invoke(env.History);
                else
                    MessageReceived?.Invoke(env);
            }
        }
        catch
        {
            // 연결 끊김
        }
        if (myGeneration == _generation) Disconnected?.Invoke();
    }

    /// <summary>사용자 요청으로 연결을 끊는다. 재연결은 ConnectAsync를 다시 호출하면 된다.</summary>
    public void Disconnect()
    {
        _generation++; // 옛 수신 루프가 이 세대를 더 이상 최신으로 보지 않게 함
        try { _tcp?.Close(); } catch { /* 무시 */ }
        _tcp = null;
        _writer = null;
        _reader = null;
    }

    public async Task SendAsync(string text, ChatTarget target)
    {
        if (_writer == null) return;
        await LineProtocol.SendAsync(_writer, new ChatEnvelope { Type = MsgType.Chat, Target = target, Text = text }, _key);
    }

    public async Task SendImageAsync(byte[] pngBytes, ChatTarget target)
    {
        if (_writer == null) return;
        await LineProtocol.SendAsync(_writer, new ChatEnvelope { Type = MsgType.Chat, Target = target, Image = pngBytes }, _key);
    }
}
