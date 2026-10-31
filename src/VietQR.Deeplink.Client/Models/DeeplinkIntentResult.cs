using System.Text.Json.Serialization;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Kết quả tạo link mở app ngân hàng.
/// </summary>
public sealed class DeeplinkIntentResult
{
    /// <summary>
    /// Link trung gian mở app ngân hàng trên điện thoại (tự hủy sau khi dùng hoặc hết hạn).
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Thời hạn hiệu lực của link (giây).
    /// </summary>
    [JsonPropertyName("expiresInSeconds")]
    public int ExpiresInSeconds { get; set; }
}
