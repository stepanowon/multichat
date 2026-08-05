using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Shared;

namespace ChatClient;

public class MainForm : Form
{
    private readonly ChatClientCore _chat = new();
    private readonly FileClientCore _files;
    private readonly ChatHistoryStore _history = new("client_history.jsonl");
    private readonly List<ChatEnvelope> _allMessages = new();
    private readonly string _myName;

    private static readonly Font BaseFont = new("Segoe UI", 9.9f);
    private static readonly Font HeaderFont = new("Segoe UI", 12.1f, FontStyle.Bold);
    private static readonly Color AccentColor = Color.FromArgb(0, 99, 177);
    private static readonly Color PanelBg = Color.FromArgb(247, 247, 249);

    private readonly ChatLogView _chatLog = new() { Dock = DockStyle.Fill };
    private readonly TextBox _inputBox = new() { Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true, Font = BaseFont, ScrollBars = ScrollBars.Vertical };
    private readonly RadioButton _toAll = new() { Text = "전체 수강생", Checked = true, AutoSize = true, Font = BaseFont };
    private readonly RadioButton _toInstructor = new() { Text = "강사에게만", AutoSize = true, Font = BaseFont };
    private readonly Button _sendBtn = new() { Text = "전송", Width = 90, Height = 60, BackColor = AccentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = BaseFont };
    private readonly Button _exportBtn = new() { Text = "대화 내보내기 (Markdown)", AutoSize = true, Font = BaseFont };
    private readonly ListBox _fileList = new() { Dock = DockStyle.Fill, Font = BaseFont, BorderStyle = BorderStyle.None, SelectionMode = SelectionMode.MultiExtended };
    private readonly Button _refreshFilesBtn = new() { Text = "새로고침", Font = BaseFont, AutoSize = true };
    private readonly Button _downloadBtn = new() { Text = "다운로드", Font = BaseFont, AutoSize = true, BackColor = AccentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    public MainForm(string host, int chatPort, int filePort, string name)
    {
        _myName = name;
        _files = new FileClientCore(host, filePort);
        Text = $"수강생 채팅 - {name}";
        Width = 1050;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;
        Font = BaseFont;
        BackColor = Color.White;
        _sendBtn.FlatAppearance.BorderSize = 0;
        _downloadBtn.FlatAppearance.BorderSize = 0;

        BuildLayout(host, chatPort);

        foreach (var env in _history.Load()) AppendMessage(env, persist: false);

        _chat.MessageReceived += env => Invoke((MethodInvoker)(() => AppendMessage(env)));
        _chat.HistoryReceived += list => Invoke((MethodInvoker)(() =>
        {
            foreach (var e in list) AppendMessage(e);
        }));
        _chat.Disconnected += () => Invoke((MethodInvoker)(() => AppendSystem("서버와 연결이 끊어졌습니다.")));

        _sendBtn.Click += (_, _) => SendMessage();
        _inputBox.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
            else if (e.Control && e.KeyCode == Keys.V && Clipboard.ContainsImage())
            {
                e.SuppressKeyPress = true;
                SendImageFromClipboard();
            }
        };
        _exportBtn.Click += (_, _) => ExportMarkdown();
        _refreshFilesBtn.Click += async (_, _) => await RefreshFileListAsync();
        _downloadBtn.Click += async (_, _) => await DownloadSelectedAsync();

        Load += async (_, _) =>
        {
            try
            {
                await _chat.ConnectAsync(host, chatPort, name);
                AppendSystem("서버에 접속했습니다.");
                await RefreshFileListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("서버 접속 실패: " + ex.Message);
            }
        };
    }

    private void BuildLayout(string host, int chatPort)
    {
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = PanelBg, Padding = new Padding(12, 8, 12, 8) };
        topPanel.Controls.Add(new Label { Text = $"수강생 채팅 - {_myName}", Font = HeaderFont, AutoSize = true, Location = new Point(12, 6) });
        topPanel.Controls.Add(new Label { Text = $"서버: {host}:{chatPort}", Font = BaseFont, ForeColor = Color.Gray, AutoSize = true, Location = new Point(12, 32) });

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 680, SplitterWidth = 6 };

        var bottomPanel = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 100, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 6, 0, 0) };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var targetRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        targetRow.Controls.Add(_toAll);
        targetRow.Controls.Add(_toInstructor);
        targetRow.Controls.Add(new Label { Text = "  (Ctrl+Enter로 전송, Enter는 줄바꿈, Ctrl+V로 이미지 붙여넣기)", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(6, 3, 0, 0) });
        bottomPanel.Controls.Add(targetRow, 0, 0);
        bottomPanel.SetColumnSpan(targetRow, 2);
        bottomPanel.Controls.Add(_inputBox, 0, 1);
        bottomPanel.Controls.Add(_sendBtn, 1, 1);

        var chatContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var chatBorder = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        chatBorder.Controls.Add(_chatLog);
        chatContainer.Controls.Add(chatBorder);
        chatContainer.Controls.Add(bottomPanel);
        chatContainer.Controls.Add(_exportBtn);
        _exportBtn.Dock = DockStyle.Top;
        _exportBtn.Margin = new Padding(0, 0, 0, 6);
        split.Panel1.Controls.Add(chatContainer);

        var filePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = PanelBg };
        var fileButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, FlowDirection = FlowDirection.RightToLeft };
        fileButtons.Controls.Add(_downloadBtn);
        fileButtons.Controls.Add(_refreshFilesBtn);
        var fileBorder = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        fileBorder.Controls.Add(_fileList);
        filePanel.Controls.Add(fileBorder);
        filePanel.Controls.Add(fileButtons);
        filePanel.Controls.Add(new Label { Text = "공유 파일 목록 (여러 개 선택 가능)", Font = new Font(BaseFont, FontStyle.Bold), Dock = DockStyle.Top, Height = 28 });
        split.Panel2.Controls.Add(filePanel);

        Controls.Add(split);
        Controls.Add(topPanel);
    }

    private async void SendMessage()
    {
        var text = _inputBox.Text.Trim();
        if (text.Length == 0) return;
        _inputBox.Clear();

        var target = _toInstructor.Checked ? ChatTarget.InstructorOnly : ChatTarget.All;
        AppendMessage(new ChatEnvelope { Type = MsgType.Chat, From = _myName, Target = target, Text = text, Time = DateTime.Now });
        await _chat.SendAsync(text, target);
    }

    private async void SendImageFromClipboard()
    {
        Image? img;
        try { img = Clipboard.GetImage(); } catch { img = null; }
        if (img == null) return;

        using (img)
        {
            var bytes = ImageUtil.EncodeForTransfer(img);
            var target = _toInstructor.Checked ? ChatTarget.InstructorOnly : ChatTarget.All;
            AppendMessage(new ChatEnvelope { Type = MsgType.Chat, From = _myName, Target = target, Image = bytes, Time = DateTime.Now });
            await _chat.SendImageAsync(bytes, target);
        }
    }

    private void AppendMessage(ChatEnvelope env, bool persist = true)
    {
        _allMessages.Add(env);
        var isMine = env.From == _myName;
        if (env.Image != null)
            _chatLog.AppendImageMessage(env.Type, env.From, env.Target, env.To, env.Image, env.Time, isMine);
        else
            _chatLog.AppendMessage(env.Type, env.From, env.Target, env.To, env.Text, env.Time, isMine);

        if (persist) _history.Append(env);
    }

    private void AppendSystem(string text) =>
        AppendMessage(new ChatEnvelope { Type = MsgType.System, From = "시스템", Text = text, Time = DateTime.Now });

    private void ExportMarkdown()
    {
        using var dlg = new SaveFileDialog { Filter = "Markdown|*.md", FileName = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}.md" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine($"# 채팅 기록 ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine();
        foreach (var m in _allMessages)
        {
            var target = m.Target switch
            {
                ChatTarget.InstructorOnly => " (강사에게만)",
                ChatTarget.Specific => $" (개인 메시지{(m.To != null ? " → " + m.To : "")})",
                _ => ""
            };
            sb.AppendLine($"- **[{m.Time:HH:mm:ss}] {m.From}{target}**:");
            sb.AppendLine();
            if (m.Image != null)
            {
                sb.AppendLine("  (이미지 메시지 - 내보내기에는 포함되지 않음)");
            }
            else
            {
                foreach (var line in m.Text.Replace("\r\n", "\n").Split('\n'))
                    sb.AppendLine($"  {line}");
            }
            sb.AppendLine();
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), LineProtocol.NoBom);
        MessageBox.Show("내보내기가 완료되었습니다.");
    }

    private async Task RefreshFileListAsync()
    {
        try
        {
            var files = await _files.ListAsync();
            _fileList.Items.Clear();
            foreach (var f in files) _fileList.Items.Add(new FileItem(f.Name, f.Size));
        }
        catch (Exception ex)
        {
            MessageBox.Show("파일 목록을 가져오지 못했습니다: " + ex.Message);
        }
    }

    private async Task DownloadSelectedAsync()
    {
        var items = _fileList.SelectedItems.Cast<FileItem>().ToList();
        if (items.Count == 0) return;

        if (items.Count == 1)
        {
            using var dlg = new SaveFileDialog { FileName = items[0].Name };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                await _files.DownloadAsync(items[0].Name, dlg.FileName);
                MessageBox.Show("다운로드가 완료되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("다운로드 실패: " + ex.Message);
            }
            return;
        }

        using var folderDlg = new FolderBrowserDialog { Description = "다운로드할 폴더를 선택하세요." };
        if (folderDlg.ShowDialog() != DialogResult.OK) return;

        int ok = 0, fail = 0;
        foreach (var item in items)
        {
            try
            {
                await _files.DownloadAsync(item.Name, Path.Combine(folderDlg.SelectedPath, item.Name));
                ok++;
            }
            catch { fail++; }
        }
        MessageBox.Show($"다운로드 완료: {ok}개 성공, {fail}개 실패");
    }
}

internal record FileItem(string Name, long Size)
{
    public override string ToString() => $"{Name} ({Size / 1024.0:0.#} KB)";
}
