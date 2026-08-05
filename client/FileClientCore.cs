using System.Net.Sockets;
using System.Text;
using Shared;

namespace ChatClient;

public record FileEntry(string Name, long Size);

/// <summary>서버의 파일 목록/다운로드 TCP 포트를 사용하는 클라이언트. 요청마다 새 연결.</summary>
public class FileClientCore
{
    private readonly string _host;
    private readonly int _port;

    public FileClientCore(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task<List<FileEntry>> ListAsync()
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host, _port);
        var stream = tcp.GetStream();
        var writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = true };
        var reader = new StreamReader(stream, LineProtocol.NoBom);

        await writer.WriteLineAsync("LIST");

        var result = new List<FileEntry>();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null && line != "END")
        {
            var parts = line.Split('|');
            if (parts.Length == 2 && long.TryParse(parts[1], out var size))
                result.Add(new FileEntry(parts[0], size));
        }
        return result;
    }

    public async Task DownloadAsync(string fileName, string savePath)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host, _port);
        var stream = tcp.GetStream();
        var writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = true };
        await writer.WriteLineAsync($"GET|{fileName}");

        // 이 뒤로 바이너리가 이어지므로 StreamReader(버퍼링)를 쓰지 않고 원시 스트림을 직접 읽는다.
        var status = await ReadRawLineAsync(stream);
        if (status != "OK") throw new IOException("파일을 가져오지 못했습니다: " + status);

        var lenBuf = new byte[8];
        await ReadExactAsync(stream, lenBuf, 8);
        long len = BitConverter.ToInt64(lenBuf, 0);

        using var fs = File.Create(savePath);
        long remaining = len;
        var buf = new byte[81920];
        while (remaining > 0)
        {
            int read = await stream.ReadAsync(buf.AsMemory(0, (int)Math.Min(buf.Length, remaining)));
            if (read == 0) throw new IOException("연결이 끊겼습니다.");
            await fs.WriteAsync(buf.AsMemory(0, read));
            remaining -= read;
        }
    }

    /// <summary>서버 세션의 채팅 암호화 키를 받아온다. 서버가 시작되어 있어야 한다.</summary>
    public async Task<byte[]> GetKeyAsync()
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host, _port);
        var stream = tcp.GetStream();
        var writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = true };
        await writer.WriteLineAsync("GETKEY");

        var status = await ReadRawLineAsync(stream);
        if (status != "OK") throw new IOException("암호화 키를 가져오지 못했습니다: " + status);

        var keyBuf = new byte[CryptoUtil.KeySizeBytes];
        await ReadExactAsync(stream, keyBuf, keyBuf.Length);
        return keyBuf;
    }

    private static async Task<string> ReadRawLineAsync(NetworkStream stream)
    {
        var sb = new StringBuilder();
        var b = new byte[1];
        while (true)
        {
            int n = await stream.ReadAsync(b.AsMemory(0, 1));
            if (n == 0) break;
            if (b[0] == '\n') break;
            if (b[0] != '\r') sb.Append((char)b[0]);
        }
        return sb.ToString();
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buf, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buf.AsMemory(offset, count - offset));
            if (read == 0) throw new IOException("연결이 끊겼습니다.");
            offset += read;
        }
    }
}
