using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using Shared;

namespace ChatServer;

public class MainForm : Form
{
    private const string AllStudentsOption = "(전체 수강생)";

    private readonly ChatServerCore _chat = new();
    private readonly FileServerCore _files = new();
    private readonly ChatHistoryStore _history = new("server_history.jsonl");

    private static readonly Font BaseFont = new("Segoe UI", 9.9f);
    private static readonly Font HeaderFont = new("Segoe UI", 12.1f, FontStyle.Bold);
    private static readonly Color AccentColor = Color.FromArgb(0, 99, 177);
    private static readonly Color PanelBg = Color.FromArgb(247, 247, 249);

    private readonly TextBox _chatPortBox = new() { Text = "9000", Width = 60, Font = BaseFont };
    private readonly TextBox _filePortBox = new() { Text = "9001", Width = 60, Font = BaseFont };
    private readonly TextBox _folderBox = new() { Width = 260, ReadOnly = true, Font = BaseFont };
    private readonly Button _browseBtn = new() { Text = "폴더 선택...", Font = BaseFont, AutoSize = true };
    private readonly Button _startBtn = new() { Text = "서버 시작", Font = BaseFont, Height = 32, Width = 110 };
    private readonly Label _addressLabel = new() { Text = "서버 주소: (시작 전)", Font = BaseFont, AutoSize = true, ForeColor = AccentColor };

    private readonly ListBox _clientList = new() { Dock = DockStyle.Fill, Font = BaseFont, BorderStyle = BorderStyle.None };
    private readonly ChatLogView _chatLog = new() { Dock = DockStyle.Fill };
    private readonly TextBox _inputBox = new() { Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true, Font = BaseFont, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _targetCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = BaseFont, Width = 160 };
    private readonly Button _sendBtn = new() { Text = "전송", Font = BaseFont, Width = 90, Height = 60, BackColor = AccentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    public MainForm()
    {
        Text = "채팅 서버 (강사용)";
        Width = 1050;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;
        Font = BaseFont;
        BackColor = Color.White;
        _sendBtn.FlatAppearance.BorderSize = 0;
        _targetCombo.Items.Add(AllStudentsOption);
        _targetCombo.SelectedIndex = 0;

        BuildLayout();

        foreach (var env in _history.Load()) AppendMessage(env, isMine: env.From == Names.Instructor, persist: false);

        _chat.Log += AppendSystem;
        _chat.MessageArrived += env => Invoke((MethodInvoker)(() => AppendMessage(env, isMine: false)));
        _chat.ClientListChanged += _ => Invoke((MethodInvoker)RefreshClientList);
        _files.Log += AppendSystem;

        _startBtn.Click += (_, _) => StartServer();
        _browseBtn.Click += (_, _) => BrowseFolder();
        _sendBtn.Click += (_, _) => SendInstructorMessage();
        _inputBox.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendInstructorMessage();
            }
            else if (e.Control && e.KeyCode == Keys.V && Clipboard.ContainsImage())
            {
                e.SuppressKeyPress = true;
                SendInstructorImageFromClipboard();
            }
        };
    }

    private void BuildLayout()
    {
        var topPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = PanelBg };
        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12, 8, 12, 10)
        };
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label { Text = "강사용 채팅 서버", Font = HeaderFont, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };

        var settingsFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        settingsFlow.Controls.AddRange(new Control[]
        {
            new Label { Text = "채팅 포트:", AutoSize = true, Padding = new Padding(0, 7, 4, 0) }, _chatPortBox,
            new Label { Text = "파일 포트:", AutoSize = true, Padding = new Padding(10, 7, 4, 0) }, _filePortBox,
            new Label { Text = "공유 폴더:", AutoSize = true, Padding = new Padding(10, 7, 4, 0) }, _folderBox,
            _browseBtn, _startBtn
        });

        _addressLabel.AutoSize = true;
        _addressLabel.Font = new Font(BaseFont, FontStyle.Bold);

        topLayout.Controls.Add(titleLabel, 0, 0);
        topLayout.Controls.Add(settingsFlow, 0, 1);
        topLayout.Controls.Add(_addressLabel, 0, 2);
        topPanel.Controls.Add(topLayout);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 780, SplitterWidth = 6 };

        var chatContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var bottomPanel = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 100, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 6, 0, 0) };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var targetRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        targetRow.Controls.Add(new Label { Text = "받는 사람:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        targetRow.Controls.Add(_targetCombo);
        targetRow.Controls.Add(new Label { Text = "  (Ctrl+Enter로 전송, Enter는 줄바꿈, Ctrl+V로 이미지 붙여넣기)", AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(6, 6, 0, 0) });
        bottomPanel.Controls.Add(targetRow, 0, 0);
        bottomPanel.SetColumnSpan(targetRow, 2);
        bottomPanel.Controls.Add(_inputBox, 0, 1);
        bottomPanel.Controls.Add(_sendBtn, 1, 1);

        var chatBorder = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        chatBorder.Controls.Add(_chatLog);
        chatContainer.Controls.Add(chatBorder);
        chatContainer.Controls.Add(bottomPanel);
        split.Panel1.Controls.Add(chatContainer);

        var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = PanelBg };
        var clientBorder = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        clientBorder.Controls.Add(_clientList);
        rightPanel.Controls.Add(clientBorder);
        rightPanel.Controls.Add(new Label { Text = "접속한 수강생", Font = new Font(BaseFont, FontStyle.Bold), Dock = DockStyle.Top, Height = 28 });
        split.Panel2.Controls.Add(rightPanel);

        Controls.Add(split);
        Controls.Add(topPanel);
    }

    private void StartServer()
    {
        if (!int.TryParse(_chatPortBox.Text, out var chatPort) || !int.TryParse(_filePortBox.Text, out var filePort))
        {
            MessageBox.Show("포트 번호를 확인하세요.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_folderBox.Text) || !Directory.Exists(_folderBox.Text))
        {
            MessageBox.Show("공유 폴더를 선택하세요.");
            return;
        }

        var key = CryptoUtil.GenerateKey();
        _files.SharedFolder = _folderBox.Text;
        _files.SessionKey = key;
        _chat.Start(chatPort, key);
        _files.Start(filePort);

        _startBtn.Enabled = false;
        _chatPortBox.Enabled = _filePortBox.Enabled = _browseBtn.Enabled = false;

        var addresses = GetLocalIPv4Addresses();
        _addressLabel.Text = addresses.Count > 0
            ? $"서버 주소: {string.Join(", ", addresses)}  (채팅 포트 {chatPort} / 파일 포트 {filePort})"
            : $"서버 주소: 확인 불가 (채팅 포트 {chatPort} / 파일 포트 {filePort})";

        AppendSystem("서버가 시작되었습니다.");
    }

    private static List<string> GetLocalIPv4Addresses()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                .Select(ip => ip.ToString())
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK) _folderBox.Text = dlg.SelectedPath;
    }

    private async void SendInstructorMessage()
    {
        var text = _inputBox.Text.Trim();
        if (text.Length == 0) return;

        var targetName = _targetCombo.SelectedItem as string;
        var isAll = targetName == null || targetName == AllStudentsOption;

        _inputBox.Clear();

        var env = new ChatEnvelope
        {
            Type = MsgType.Chat,
            From = Names.Instructor,
            Target = isAll ? ChatTarget.All : ChatTarget.Specific,
            To = isAll ? null : targetName,
            Text = text,
            Time = DateTime.Now
        };
        AppendMessage(env, isMine: true);

        var ok = await _chat.SendInstructorMessageAsync(text, isAll ? null : targetName);
        if (!ok) AppendSystem($"'{targetName}' 님에게 전송하지 못했습니다 (접속 종료됨).");
    }

    private async void SendInstructorImageFromClipboard()
    {
        Image? img;
        try { img = Clipboard.GetImage(); } catch { img = null; }
        if (img == null) return;

        using (img)
        {
            var bytes = ImageUtil.EncodeForTransfer(img);
            var targetName = _targetCombo.SelectedItem as string;
            var isAll = targetName == null || targetName == AllStudentsOption;

            var env = new ChatEnvelope
            {
                Type = MsgType.Chat,
                From = Names.Instructor,
                Target = isAll ? ChatTarget.All : ChatTarget.Specific,
                To = isAll ? null : targetName,
                Image = bytes,
                Time = DateTime.Now
            };
            AppendMessage(env, isMine: true);

            var ok = await _chat.SendInstructorImageAsync(bytes, isAll ? null : targetName);
            if (!ok) AppendSystem($"'{targetName}' 님에게 이미지를 전송하지 못했습니다 (접속 종료됨).");
        }
    }

    private void AppendMessage(ChatEnvelope env, bool isMine, bool persist = true)
    {
        if (env.Image != null)
            _chatLog.AppendImageMessage(env.Type, env.From, env.Target, env.To, env.Image, env.Time, isMine);
        else
            _chatLog.AppendMessage(env.Type, env.From, env.Target, env.To, env.Text, env.Time, isMine);

        if (persist) _history.Append(env);
    }

    private void AppendSystem(string text)
    {
        if (InvokeRequired) { Invoke((MethodInvoker)(() => AppendSystem(text))); return; }
        AppendMessage(new ChatEnvelope { Type = MsgType.System, From = "시스템", Text = text, Time = DateTime.Now }, isMine: false);
    }

    private void RefreshClientList()
    {
        var names = _chat.GetClientNames();

        _clientList.Items.Clear();
        foreach (var name in names) _clientList.Items.Add(name);

        var previouslySelected = _targetCombo.SelectedItem as string;
        _targetCombo.Items.Clear();
        _targetCombo.Items.Add(AllStudentsOption);
        foreach (var name in names) _targetCombo.Items.Add(name);
        _targetCombo.SelectedIndex = previouslySelected != null && _targetCombo.Items.Contains(previouslySelected)
            ? _targetCombo.Items.IndexOf(previouslySelected)
            : 0;
    }
}
