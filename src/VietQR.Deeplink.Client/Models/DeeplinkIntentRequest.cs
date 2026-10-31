using System.Text.Json.Serialization;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Dữ liệu gửi lên server để tạo link mở app.
/// </summary>
public sealed class DeeplinkIntentRequest
{
    /// <summary>
    /// Mã app hoặc BIN ngân hàng (ví dụ: MB, TPB, 970422).
    /// </summary>
    [JsonPropertyName("appCode")]
    public string AppCode { get; set; } = string.Empty;

    /// <summary>
    /// Chuỗi mã QR thanh toán chuẩn EMVCo/NAPAS247.
    /// </summary>
    [JsonPropertyName("qrContent")]
    public string QrContent { get; set; } = string.Empty;

    public DeeplinkIntentRequest()
    {
    }

    public DeeplinkIntentRequest(string appCode, string qrContent)
    {
        AppCode = appCode ?? string.Empty;
        QrContent = qrContent ?? string.Empty;
    }
}
