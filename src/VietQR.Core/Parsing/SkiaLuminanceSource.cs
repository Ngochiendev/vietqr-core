using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace VietQR.Core;

/// <summary>
/// Chuyển đổi dữ liệu điểm ảnh từ SkiaSharp SKBitmap sang ZXing LuminanceSource để nhận dạng mã QR.
/// </summary>
internal sealed class SkiaLuminanceSource : BaseLuminanceSource
{
    public SkiaLuminanceSource(SKBitmap bitmap) : base(bitmap.Width, bitmap.Height)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        var luminances = new byte[width * height];

        // Đọc từng điểm ảnh và chuyển đổi sang mức xám (Grayscale / Luminance)
        // Công thức tiêu chuẩn ITU-R BT.601: Y = 0.299*R + 0.587*G + 0.114*B
        var pixels = bitmap.Pixels;
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            luminances[i] = (byte)((p.Red * 299 + p.Green * 587 + p.Blue * 114) / 1000);
        }

        this.luminances = luminances;
    }

    private SkiaLuminanceSource(byte[] luminances, int width, int height) : base(width, height)
    {
        this.luminances = luminances;
    }

    protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
    {
        return new SkiaLuminanceSource(newLuminances, width, height);
    }
}

