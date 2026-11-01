using System.Text.Json.Serialization;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Thông tin ứng dụng ngân hàng hoặc ví điện tử được cổng Deeplink hỗ trợ.
/// </summary>
public sealed class DeeplinkBankAppInfo
{
    /// <summary>
    /// Mã định danh ứng dụng dùng khi tạo link điều hướng (VD: MB, VCB, TPB, MOMO).
    /// </summary>
    [JsonPropertyName("appCode")]
    public string AppCode { get; set; } = string.Empty;

    /// <summary>
    /// Mã BIN ngân hàng 6 chữ số theo chuẩn NAPAS (nếu có).
    /// </summary>
    [JsonPropertyName("bin")]
    public string? Bin { get; set; }

    /// <summary>
    /// Tên viết tắt của ngân hàng hoặc ví điện tử.
    /// </summary>
    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chính thức của ứng dụng trên App Store / Google Play.
    /// </summary>
    [JsonPropertyName("appName")]
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh biểu trưng (Logo) của ngân hàng hoặc ví điện tử.
    /// </summary>
    [JsonPropertyName("logo")]
    public string Logo { get; set; } = string.Empty;

    /// <summary>
    /// Cho biết ứng dụng có hỗ trợ tự động điền STK, số tiền, nội dung khi mở qua deep link hay không.
    /// </summary>
    [JsonPropertyName("supportsAutofill")]
    public bool SupportsAutofill { get; set; }
}
