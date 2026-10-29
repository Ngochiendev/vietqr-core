namespace VietQR.Core;

/// <summary>
/// Tiện ích chuyển đổi và lưu ảnh mã VietQR (Base64, Bytes, File ảnh, HTML Image Tag).
/// Giúp lập trình viên dễ dàng hiển thị ảnh trên Web, App mobile, desktop hoặc lưu ra file PNG.
/// </summary>
public static class VietQrImageConverter
{
    /// <summary>
    /// Chuyển đổi chuỗi Base64 hoặc chuỗi Data URL (data:image/png;base64,...) thành mảng byte PNG nguyên bản.
    /// Tự động tách bỏ tiền tố MIME an toàn, không lo bị lỗi FormatException.
    /// </summary>
    /// <param name="base64OrDataUrl">Chuỗi Base64 thuần hoặc Data URL đầy đủ.</param>
    /// <returns>Mảng byte của file ảnh PNG.</returns>
    public static byte[] ToBytes(string base64OrDataUrl)
    {
        if (string.IsNullOrWhiteSpace(base64OrDataUrl))
        {
            throw new ArgumentException("Chuỗi Base64 không được để trống.", nameof(base64OrDataUrl));
        }

        var trimmed = base64OrDataUrl.Trim();
        int commaIndex = trimmed.IndexOf(',');
        string cleanBase64 = (commaIndex >= 0 && trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            ? trimmed[(commaIndex + 1)..]
            : trimmed;

        return Convert.FromBase64String(cleanBase64);
    }

    /// <summary>
    /// Chuyển chuỗi Base64 hoặc Data URL thành MemoryStream để thuận tiện trả về từ ASP.NET Core FileStreamResult.
    /// </summary>
    /// <param name="base64OrDataUrl">Chuỗi Base64 thuần hoặc Data URL.</param>
    /// <returns>MemoryStream chứa dữ liệu ảnh PNG.</returns>
    public static MemoryStream ToStream(string base64OrDataUrl)
    {
        return new MemoryStream(ToBytes(base64OrDataUrl));
    }

    /// <summary>
    /// Lưu chuỗi Base64 hoặc Data URL trực tiếp thành file ảnh PNG trên ổ đĩa.
    /// Tự động tạo thư mục cha nếu thư mục chưa tồn tại.
    /// </summary>
    /// <param name="base64OrDataUrl">Chuỗi Base64 hoặc Data URL.</param>
    /// <param name="filePath">Đường dẫn file cần lưu (VD: "exports/qr_payment.png").</param>
    /// <param name="ct">Token hủy tác vụ bất đồng bộ.</param>
    public static async Task SaveToFileAsync(string base64OrDataUrl, string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Đường dẫn file (filePath) không được để trống.", nameof(filePath));
        }

        var bytes = ToBytes(base64OrDataUrl);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(filePath, bytes, ct);
    }

    /// <summary>
    /// Lưu mảng byte ảnh trực tiếp thành file trên ổ đĩa.
    /// Tự động tạo thư mục cha nếu thư mục chưa tồn tại.
    /// </summary>
    /// <param name="imageBytes">Mảng byte ảnh PNG.</param>
    /// <param name="filePath">Đường dẫn file cần lưu.</param>
    /// <param name="ct">Token hủy tác vụ bất đồng bộ.</param>
    public static async Task SaveToFileAsync(byte[] imageBytes, string filePath, CancellationToken ct = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            throw new ArgumentException("Mảng byte ảnh không được rỗng.", nameof(imageBytes));
        }
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Đường dẫn file (filePath) không được để trống.", nameof(filePath));
        }

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(filePath, imageBytes, ct);
    }

    /// <summary>
    /// Chuyển mảng byte ảnh PNG thành chuỗi Data URL Base64 hoàn chỉnh (dùng trực tiếp cho thẻ img src trên Web).
    /// </summary>
    public static string ToDataUrl(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            throw new ArgumentException("Mảng byte ảnh không được rỗng.", nameof(imageBytes));
        }
        return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
    }

    /// <summary>
    /// Sinh chuỗi thẻ HTML &lt;img src="..." /&gt; hoàn chỉnh để nhúng hiển thị ngay lập tức lên giao diện Web/Email.
    /// </summary>
    /// <param name="base64OrDataUrl">Chuỗi Base64 hoặc Data URL.</param>
    /// <param name="alt">Mô tả alt của ảnh.</param>
    /// <param name="width">Chiều rộng hiển thị (pixel, tùy chọn).</param>
    /// <param name="height">Chiều cao hiển thị (pixel, tùy chọn).</param>
    public static string ToHtmlImgTag(string base64OrDataUrl, string alt = "VietQR Payment", int? width = null, int? height = null)
    {
        var trimmed = base64OrDataUrl?.Trim() ?? "";
        var dataUrl = trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"data:image/png;base64,{trimmed}";

        var styleAttr = (width.HasValue || height.HasValue)
            ? $" style=\"{(width.HasValue ? $"width:{width}px;" : "")}{(height.HasValue ? $"height:{height}px;" : "")}\""
            : "";

        return $"<img src=\"{dataUrl}\" alt=\"{alt}\"{styleAttr} />";
    }
}

