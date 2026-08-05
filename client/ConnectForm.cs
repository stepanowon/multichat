using System.Drawing;
using System.Windows.Forms;
using Shared;

namespace ChatClient;

public class ConnectForm : Form
{
    public string Host { get; private set; } = "";
    public int ChatPort { get; private set; }
    public int FilePort { get; private set; }
    public string UserName { get; private set; } = "";

    private readonly TextBox _hostBox = new() { Text = "127.0.0.1", Width = 200 };
    private readonly TextBox _chatPortBox = new() { Text = "9000", Width = 60 };
    private readonly TextBox _filePortBox = new() { Text = "9001", Width = 60 };
    private readonly TextBox _nameBox = new() { Width = 200 };
    private readonly Button _okBtn = new() { Text = "접속" };

    public ConnectForm()
    {
        Text = "서버 접속";
        Width = 360;
        Height = 260;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.9f);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(15) };
        layout.Controls.Add(new Label { Text = "서버 주소:", AutoSize = true }, 0, 0);
        layout.Controls.Add(_hostBox, 1, 0);
        layout.Controls.Add(new Label { Text = "채팅 포트:", AutoSize = true }, 0, 1);
        layout.Controls.Add(_chatPortBox, 1, 1);
        layout.Controls.Add(new Label { Text = "파일 포트:", AutoSize = true }, 0, 2);
        layout.Controls.Add(_filePortBox, 1, 2);
        layout.Controls.Add(new Label { Text = "이름:", AutoSize = true }, 0, 3);
        layout.Controls.Add(_nameBox, 1, 3);
        layout.Controls.Add(_okBtn, 1, 4);
        Controls.Add(layout);

        AcceptButton = _okBtn;
        _okBtn.Click += OkBtn_Click;
    }

    private void OkBtn_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_hostBox.Text) || string.IsNullOrWhiteSpace(_nameBox.Text) ||
            !int.TryParse(_chatPortBox.Text, out var cp) || !int.TryParse(_filePortBox.Text, out var fp))
        {
            MessageBox.Show("모든 항목을 올바르게 입력하세요.");
            return;
        }
        if (_nameBox.Text.Trim() == Names.Instructor)
        {
            MessageBox.Show("사용할 수 없는 이름입니다.");
            return;
        }

        Host = _hostBox.Text.Trim();
        ChatPort = cp;
        FilePort = fp;
        UserName = _nameBox.Text.Trim();
        DialogResult = DialogResult.OK;
    }
}
