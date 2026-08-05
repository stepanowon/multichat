using System.Net.Sockets;
using Shared;

namespace ChatClient;

/// <summary>서버와의 채팅 TCP 연결 하나를 관리한다. 연결 유지 + 백그라운드 수신 루프.</summary>
public class ChatClientCore
{
    private TcpClient? _tcp;
    private StreamWriter? _writer;
    private StreamReader? _reader;

    public event Action<ChatEnvelope>? MessageReceived;
    public event Action<List<ChatEnvelope>>? HistoryReceived;
    public event Action? Disconnected;

    public async Task ConnectAsync(string host, int port, string name)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port);
        var stream = _tcp.GetStream();
        _reader = new StreamReader(stream, LineProtocol.NoBom);
        _writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = false };

        await LineProtocol.SendAsync(_writer, new ChatEnvelope { Type = MsgType.Join, From = name });

        _ = ReceiveLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (true)
            {
                var env = await LineProtocol.ReceiveAsync(_reader!);
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
        Disconnected?.Invoke();
    }

    public async Task SendAsync(string text, ChatTarget target)
    {
        if (_writer == null) return;
        await LineProtocol.SendAsync(_writer, new ChatEnvelope { Type = MsgType.Chat, Target = target, Text = text });
    }

    public async Task SendImageAsync(byte[] pngBytes, ChatTarget target)
    {
        if (_writer == null) return;
        await LineProtocol.SendAsync(_writer, new ChatEnvelope { Type = MsgType.Chat, Target = target, Image = pngBytes });
    }
}
