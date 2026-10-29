using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace VietQR.Core;

/// <summary>
/// Bộ giải mã hình ảnh mã QR (Decoder) sang thông tin thanh toán VietQR.
/// Hỗ trợ đọc ảnh từ byte array, file stream, camera buffer và đối tượng SkiaSharp SKBitmap
/// ở mọi định dạng phổ biến (PNG, WebP, JPEG, BMP, GIF,...).
/// </summary>
public static class VietQrDecoder
{
    /// <summary>
    /// Đọc chuỗi văn bản thô từ mã QR trong đối tượng <see cref="SKBitmap"/>.
    /// Trả về null nếu không tìm thấy mã QR trong ảnh.
    /// </summary>
    public static string? ReadRawQrText(SKBitmap? bitmap)
    {
        if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return null;
        }

        try
        {
            var luminanceSource = new SkiaLuminanceSource(bitmap);
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                }
            };

            var result = reader.Decode(luminanceSource);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Giải mã mã QR trực tiếp từ đối tượng <see cref="SKBitmap"/>.
    /// </summary>
    public static Result<VietQrInfo> Decode(SKBitmap? bitmap, VietQrParseOptions? options = null)
    {
        if (bitmap == null)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        var text = ReadRawQrText(bitmap);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        return VietQrParser.Parse(text, options);
    }

    /// <summary>
    /// Giải mã mã QR từ mảng byte ảnh (PNG, WebP, JPEG, BMP, ...).
    /// </summary>
    public static Result<VietQrInfo> Decode(byte[]? imageBytes, VietQrParseOptions? options = null)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        SKBitmap? bitmap = null;
        try
        {
            bitmap = SKBitmap.Decode(imageBytes);
        }
        catch
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        if (bitmap == null)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        using (bitmap)
        {
            return Decode(bitmap, options);
        }
    }

    /// <summary>
    /// Giải mã mã QR từ ReadOnlySpan byte ảnh.
    /// </summary>
    public static Result<VietQrInfo> Decode(ReadOnlySpan<byte> imageBytes, VietQrParseOptions? options = null)
    {
        if (imageBytes.IsEmpty)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        SKBitmap? bitmap = null;
        try
        {
            bitmap = SKBitmap.Decode(imageBytes);
        }
        catch
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        if (bitmap == null)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        using (bitmap)
        {
            return Decode(bitmap, options);
        }
    }

    /// <summary>
    /// Giải mã mã QR từ luồng dữ liệu Stream hình ảnh.
    /// </summary>
    public static Result<VietQrInfo> Decode(Stream? imageStream, VietQrParseOptions? options = null)
    {
        if (imageStream == null)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        SKBitmap? bitmap = null;
        try
        {
            bitmap = SKBitmap.Decode(imageStream);
        }
        catch
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        if (bitmap == null)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        using (bitmap)
        {
            return Decode(bitmap, options);
        }
    }

    /// <summary>
    /// Giải mã mã QR từ luồng dữ liệu Stream hình ảnh bất đồng bộ (phù hợp cho Webhook và File Upload API).
    /// </summary>
    public static async Task<Result<VietQrInfo>> DecodeAsync(
        Stream? imageStream,
        VietQrParseOptions? options = null,
        CancellationToken ct = default)
    {
        if (imageStream == null)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
            memoryStream.Position = 0;

            return Decode(memoryStream, options);
        }
        catch
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.ImageDecodeFailed);
        }
    }
}
