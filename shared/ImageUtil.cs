using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Shared;

public static class ImageUtil
{
    // ponytail: 전송량/메모리 상한. 강의실 LAN 용도로는 충분, 더 큰 해상도가 필요해지면 값만 올리면 됨.
    public const int MaxDimension = 1600;

    /// <summary>클립보드 등에서 얻은 이미지를 전송 크기 상한에 맞게 축소해 PNG로 인코딩한다.</summary>
    public static byte[] EncodeForTransfer(Image img)
    {
        using var scaled = ScaleDown(img, MaxDimension, MaxDimension);
        using var ms = new MemoryStream();
        scaled.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>원본 비율을 유지한 채 maxWidth/maxHeight 안에 들어오도록 축소한다(더 작으면 원본 크기 그대로).</summary>
    public static Bitmap ScaleDown(Image src, int maxWidth, int maxHeight)
    {
        var ratio = Math.Min(1.0, Math.Min((double)maxWidth / src.Width, (double)maxHeight / src.Height));
        int w = Math.Max(1, (int)(src.Width * ratio));
        int h = Math.Max(1, (int)(src.Height * ratio));
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(src, 0, 0, w, h);
        return bmp;
    }

    public static Bitmap Decode(byte[] pngBytes)
    {
        using var ms = new MemoryStream(pngBytes);
        using var loaded = Image.FromStream(ms);
        return new Bitmap(loaded); // 스트림과 독립적인 복사본
    }
}
