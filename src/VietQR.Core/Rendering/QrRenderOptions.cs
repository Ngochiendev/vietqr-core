namespace VietQR.Core;

/// <summary>
/// Tùy chọn render thẻ QR (template compact/qr_only/print).
/// </summary>
/// <remarks>
/// <see cref="Amount"/> dùng kiểu <c>long?</c> để nhất quán với <see cref="VietQrRequest.Amount"/>:
/// cùng một giá trị số tiền cho cả payload và phần hiển thị trên thẻ.
/// </remarks>
public class QrRenderOptions
{
    public string Template { get; set; } = "compact";
    public string? BankBin { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountName { get; set; }

    /// <summary>Số tiền VNĐ hiển thị trên thẻ (in khi template "print", > 0 cho QR động).</summary>
    public long? Amount { get; set; }

    public string? Description { get; set; }
    public int Size { get; set; } = 500;
}
