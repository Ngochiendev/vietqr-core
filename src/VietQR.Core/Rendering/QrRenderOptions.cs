namespace VietQR.Core;

/// <summary>
/// Tùy chọn render thẻ QR (template compact/qr_only/print) và nhận diện thương hiệu.
/// </summary>
public class QrRenderOptions
{
    /// <summary>
    /// Mẫu giao diện thẻ: "compact" (mặc định), "qr_only" hoặc "print".
    /// </summary>
    public string Template { get; set; } = "compact";

    public string? BankBin { get; set; }
    public string? BankName { get; set; }
    public string? BankFullName { get; set; }
    public string? BankLogoUrl { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountName { get; set; }

    /// <summary>Số tiền VNĐ hiển thị trên thẻ (in khi template "print", > 0 cho QR động).</summary>
    public long? Amount { get; set; }

    public string? Description { get; set; }
    public string? BillNumber { get; set; }

    /// <summary>Loại dịch vụ chuyển tiền: Tài khoản (mặc định) hoặc Thẻ.</summary>
    public VietQrServiceType ServiceType { get; set; } = VietQrServiceType.Account;

    /// <summary>Kích thước ảnh vuông khi dùng template "qr_only" (pixel, mặc định 500).</summary>
    public int Size { get; set; } = 500;

    /// <summary>
    /// Định dạng ảnh xuất ra: Png (mặc định), Webp hoặc Jpeg.
    /// </summary>
    public QrImageFormat Format { get; set; } = QrImageFormat.Png;

    /// <summary>
    /// Chất lượng nén ảnh từ 1 đến 100 (áp dụng cho Webp và Jpeg, mặc định 100).
    /// </summary>
    public int Quality { get; set; } = 100;

    /// <summary>
    /// Logo thương hiệu tùy biến nhúng vào chính giữa mã QR (mảng byte PNG).
    /// Nếu để null, hệ thống sẽ dùng icon VietQR mặc định.
    /// </summary>
    public byte[]? CustomCenterLogo { get; set; }

    /// <summary>
    /// Logo thương hiệu tùy biến ở dạng chuỗi Base64 (ví dụ: "data:image/png;base64,..." hoặc raw base64).
    /// Giúp đối tác/cửa hàng dễ dàng truyền ảnh logo của họ mà không cần tự chuyển sang byte[].
    /// </summary>
    public string? CustomCenterLogoBase64
    {
        get => CustomCenterLogo != null ? Convert.ToBase64String(CustomCenterLogo) : null;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                CustomCenterLogo = null;
                return;
            }
            var clean = value.Trim();
            int commaIdx = clean.IndexOf(',');
            if (commaIdx >= 0)
            {
                clean = clean.Substring(commaIdx + 1);
            }
            CustomCenterLogo = Convert.FromBase64String(clean);
        }
    }

    /// <summary>
    /// Tỷ lệ kích thước của logo trung tâm so với mã QR (mặc định 0.18f ~ 18%, giới hạn an toàn 0.10f - 0.28f).
    /// </summary>
    public float CenterLogoRatio { get; set; } = 0.18f;
}
