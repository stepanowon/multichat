using System.Drawing;
using System.Windows.Forms;

namespace Shared;

/// <summary>
/// 채팅 로그 표시 전용 컨트롤. 메시지마다 "말풍선"(Panel)을 세로로 쌓는다.
/// RichTextBox의 관리 코드 이미지 임베드(Paste)는 신뢰성이 낮아 이 방식을 사용한다.
/// 텍스트 말풍선은 더블클릭하면 복사된다. 이미지는 썸네일로 보이며 클릭하면 원본 크기로 보기가 뜨고,
/// 우클릭 메뉴로 다운로드/복사할 수 있다.
/// </summary>
public class ChatLogView : Panel
{
    // ponytail: 말풍선 최대 폭 고정값. 패널 폭에 따라 동적으로 다시 흐르게(reflow) 하려면
    // Resize 이벤트에서 각 말풍선의 MaximumSize를 갱신해야 함 - 강의실 데스크톱 해상도에선 불필요.
    private const int MaxBubbleWidth = 640;
    // 썸네일은 원본 비율과 무관하게 항상 이 고정 박스 크기로 표시한다(Zoom 모드로 안쪽에 맞춰 축소).
    private const int ThumbBoxWidth = 180;
    private const int ThumbBoxHeight = 135;

    private static readonly Color OwnColor = Color.FromArgb(0, 99, 177);
    private static readonly Color InstructorColor = Color.FromArgb(198, 40, 40);
    private static readonly Color SystemColor = Color.Gray;
    private static readonly Color DefaultColor = Color.FromArgb(33, 33, 33);
    private static readonly Color BubbleBg = Color.FromArgb(245, 245, 247);

    private readonly FlowLayoutPanel _flow;

    public ChatLogView()
    {
        AutoScroll = true;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.9f);

        _flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8)
        };
        Controls.Add(_flow);
    }

    public void AppendMessage(MsgType type, string sender, ChatTarget target, string? to, string text, DateTime time, bool isMine)
    {
        var bubble = BuildBubbleShell(type, sender, target, to, time, isMine, out var content);
        content.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(MaxBubbleWidth - 24, 0),
            Font = Font,
            ForeColor = DefaultColor
        });
        FinishBubble(bubble, text, null);
    }

    public void AppendImageMessage(MsgType type, string sender, ChatTarget target, string? to, byte[] pngBytes, DateTime time, bool isMine)
    {
        var bubble = BuildBubbleShell(type, sender, target, to, time, isMine, out var content);

        using var original = ImageUtil.Decode(pngBytes);
        var thumb = ImageUtil.ScaleDown(original, ThumbBoxWidth, ThumbBoxHeight);
        var pic = new PictureBox
        {
            Image = thumb,
            SizeMode = PictureBoxSizeMode.Zoom, // 원본 비율 유지한 채 고정 박스 안에 맞춰 축소/레터박스
            Width = ThumbBoxWidth,
            Height = ThumbBoxHeight,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        pic.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ViewImage(pngBytes); };
        content.Controls.Add(pic);
        FinishBubble(bubble, "[이미지]", pngBytes);
    }

    private Panel BuildBubbleShell(MsgType type, string sender, ChatTarget target, string? to, DateTime time, bool isMine, out FlowLayoutPanel content)
    {
        var color = type == MsgType.System ? SystemColor
            : sender == Names.Instructor ? InstructorColor
            : isMine ? OwnColor
            : DefaultColor;

        var label = target switch
        {
            ChatTarget.InstructorOnly => " (강사에게만)",
            ChatTarget.Specific when to != null => $" (개인 메시지 → {to})",
            ChatTarget.Specific => " (개인 메시지)",
            _ => ""
        };

        var bubble = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BubbleBg,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(4, 4, 4, 4),
            MaximumSize = new Size(MaxBubbleWidth, 0)
        };
        content = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var header = new Label
        {
            Text = $"[{time:MM/dd HH:mm:ss}] {sender}{label}",
            AutoSize = true,
            Font = new Font(Font, type == MsgType.System ? (FontStyle.Bold | FontStyle.Italic) : FontStyle.Bold),
            ForeColor = color,
            Margin = new Padding(0, 0, 0, 4)
        };
        content.Controls.Add(header);
        bubble.Controls.Add(content);
        return bubble;
    }

    private void FinishBubble(Panel bubble, string rawText, byte[]? imageBytes)
    {
        var menu = new ContextMenuStrip();
        if (imageBytes != null)
        {
            menu.Items.Add(new ToolStripMenuItem("다운로드", null, (_, _) => DownloadImage(imageBytes)));
            menu.Items.Add(new ToolStripMenuItem("이미지 복사", null, (_, _) => CopyMessage(rawText, imageBytes)));
        }
        else
        {
            menu.Items.Add(new ToolStripMenuItem("메시지 복사", null, (_, _) => CopyMessage(rawText, null)));
        }
        bubble.ContextMenuStrip = menu; // 자식 컨트롤에 개별 메뉴가 없으면 WinForms가 부모 메뉴로 자동 위임함

        if (imageBytes == null)
        {
            void CopyHandler(object? s, EventArgs e) => CopyMessage(rawText, null);
            bubble.DoubleClick += CopyHandler;
            foreach (var c in AllDescendants(bubble)) c.DoubleClick += CopyHandler;
        }

        _flow.Controls.Add(bubble);
        ScrollControlIntoView(bubble);
    }

    private static IEnumerable<Control> AllDescendants(Control root)
    {
        foreach (Control c in root.Controls)
        {
            yield return c;
            foreach (var d in AllDescendants(c)) yield return d;
        }
    }

    private static void CopyMessage(string text, byte[]? imageBytes)
    {
        if (imageBytes != null)
        {
            using var bmp = ImageUtil.Decode(imageBytes);
            Clipboard.SetImage(bmp);
        }
        else
        {
            Clipboard.SetText(text);
        }
    }

    private static void DownloadImage(byte[] pngBytes)
    {
        using var dlg = new SaveFileDialog { Filter = "PNG 이미지|*.png", FileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try { File.WriteAllBytes(dlg.FileName, pngBytes); }
        catch (Exception ex) { MessageBox.Show("저장 실패: " + ex.Message); }
    }

    private static void ViewImage(byte[] pngBytes)
    {
        var img = ImageUtil.Decode(pngBytes);
        var viewer = new Form
        {
            Text = "이미지 보기",
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(Math.Min(img.Width + 20, 1200), Math.Min(img.Height + 20, 900))
        };
        var pic = new PictureBox { Image = img, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
        viewer.Controls.Add(pic);
        viewer.FormClosed += (_, _) => img.Dispose();
        viewer.Show();
    }

    public void Clear() => _flow.Controls.Clear();
}
