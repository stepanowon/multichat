using System.Net;
using System.Net.Sockets;
using Shared;

namespace ChatServer;

/// <summary>
/// 파일 목록/다운로드 + 암호화 키 배포 전용 TCP 서버. 요청마다 새 연결을 맺고 짧게 응답 후 닫는다.
/// 프로토콜: 클라이언트가 "LIST", "GET|파일명", "GETKEY" 중 한 줄을 보낸다.
///   LIST   -> "이름|바이트수" 줄들을 보내고 "END"로 종료
///   GET|x  -> "OK"(또는 "ERROR") 한 줄 + (OK인 경우) 8바이트 길이(long) + 파일 바이트
///   GETKEY -> "OK"(또는 "ERROR") 한 줄 + (OK인 경우) 채팅 암호화에 쓰는 32바이트 키
/// HttpListener 대신 순수 TCP를 쓰는 이유: 강의실 PC에서 관리자 권한/URL 예약 없이 바로 동작해야 하므로.
/// </summary>
public class FileServerCore
{
    private TcpListener? _listener;
    public string SharedFolder { get; set; } = "";
    /// <summary>이번 서버 세션의 채팅 암호화 키. GETKEY 요청에 그대로 내려준다.</summary>
    public byte[]? SessionKey { get; set; }
    public event Action<string>? Log;

    public async void Start(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Log?.Invoke($"파일 서버 시작: 포트 {port}");
        while (true)
        {
            TcpClient tcp;
            try { tcp = await _listener.AcceptTcpClientAsync(); }
            catch { break; }
            _ = HandleAsync(tcp);
        }
    }

    public void Stop() => _listener?.Stop();

    private async Task HandleAsync(TcpClient tcp)
    {
        using (tcp)
        using (var stream = tcp.GetStream())
        {
            try
            {
                var reader = new StreamReader(stream, LineProtocol.NoBom);
                var writer = new StreamWriter(stream, LineProtocol.NoBom) { AutoFlush = true };

                var line = await reader.ReadLineAsync();
                if (line == null) return;

                if (line == "LIST")
                {
                    var files = Directory.Exists(SharedFolder)
                        ? Directory.GetFiles(SharedFolder).Select(f => new FileInfo(f))
                        : Enumerable.Empty<FileInfo>();
                    foreach (var fi in files)
                        await writer.WriteLineAsync($"{fi.Name}|{fi.Length}");
                    await writer.WriteLineAsync("END");
                }
                else if (line.StartsWith("GET|"))
                {
                    var name = line[4..];
                    var path = Path.Combine(SharedFolder, name);
                    // 경로 조작(../..) 방지: 파일명만 허용
                    if (Path.GetFileName(name) != name || !File.Exists(path))
                    {
                        await writer.WriteLineAsync("ERROR");
                        return;
                    }
                    var bytes = await File.ReadAllBytesAsync(path);
                    await writer.WriteLineAsync("OK");
                    await stream.WriteAsync(BitConverter.GetBytes((long)bytes.Length));
                    await stream.WriteAsync(bytes);
                    await stream.FlushAsync();
                }
                else if (line == "GETKEY")
                {
                    if (SessionKey == null)
                    {
                        await writer.WriteLineAsync("ERROR");
                        return;
                    }
                    await writer.WriteLineAsync("OK");
                    await stream.WriteAsync(SessionKey);
                    await stream.FlushAsync();
                }
            }
            catch
            {
                // 연결 끊김 등은 무시
            }
        }
    }
}
