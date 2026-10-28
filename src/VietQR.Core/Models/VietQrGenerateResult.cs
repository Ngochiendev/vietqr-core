using System.Text.Json.Serialization;

namespace VietQR.Core;

/// <summary>
/// Dữ liệu sinh mã VietQR thành công.
/// </summary>
public class VietQrGenerateResult
{
    /// <summary>
    /// Chuỗi mã thanh toán VietQR theo chuẩn EMVCo (NAPAS247).
    /// </summary>
    /// <example>00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test63048E1D</example>
    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; } = string.Empty;

    /// <summary>
    /// Chuỗi Data URL ảnh Base64 định dạng image/png (dùng trực tiếp cho thẻ img trên Web hoặc Mobile App).
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAfQAAAH0CAYAAAD...</example>
    [JsonPropertyName("qrDataURL")]
    public string QrDataURL { get; set; } = string.Empty;
}
