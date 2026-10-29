namespace VietQR.Core;

/// <summary>
/// Kết quả giải mã chuỗi mã QR chuẩn VietQR / EMVCo / NAPAS247.
/// Chứa đầy đủ các thông tin giao dịch, ngân hàng thụ hưởng, tài khoản, số tiền và dữ liệu bán lẻ.
/// </summary>
public sealed class VietQrInfo
{
    /// <summary>
    /// Mã BIN 6 chữ số của ngân hàng thụ hưởng (ví dụ: "970422" cho MBBank, "970436" cho Vietcombank).
    /// </summary>
    public string BankBin { get; set; } = string.Empty;

    /// <summary>
    /// Thông tin ngân hàng tra cứu tự động từ danh mục NAPAS (nếu tìm thấy).
    /// </summary>
    public BankInfo? BankInfo { get; set; }

    /// <summary>
    /// Số tài khoản thanh toán hoặc số thẻ ngân hàng của người thụ hưởng.
    /// </summary>
    public string AccountNo { get; set; } = string.Empty;

    /// <summary>
    /// Loại dịch vụ: Tài khoản (QRIBFTTA) hay Thẻ (QRIBFTTC).
    /// </summary>
    public VietQrServiceType ServiceType { get; set; } = VietQrServiceType.Account;

    /// <summary>
    /// Loại mã QR: Tĩnh (Static = "11") hay Động (Dynamic = "12").
    /// </summary>
    public VietQrType QrType { get; set; } = VietQrType.Static;

    /// <summary>
    /// Cho biết đây có phải là mã QR động (đã gắn sẵn số tiền thanh toán) hay không.
    /// </summary>
    public bool IsDynamic => QrType == VietQrType.Dynamic;

    /// <summary>
    /// Số tiền giao dịch thanh toán (từ Tag 54). Null nếu là QR tĩnh.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Mã tiền tệ theo ISO 4217 (Tag 53: Mặc định "704" cho VND).
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Mã quốc gia theo ISO 3166-1 alpha 2 (Tag 58: Mặc định "VN").
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Nội dung thanh toán / chuyển khoản (từ Tag 62 Sub-tag 08).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Số hóa đơn (từ Tag 62 Sub-tag 01).
    /// </summary>
    public string? BillNumber { get; set; }

    /// <summary>
    /// Mã tham chiếu đơn hàng / Order ID cho Webhook IPN đối soát tự động (từ Tag 62 Sub-tag 05).
    /// </summary>
    public string? ReferenceLabel { get; set; }

    /// <summary>
    /// Tên điểm bán / Merchant Name (từ Tag 59 - chữ in hoa không dấu).
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Tên thành phố của merchant (từ Tag 60 - ví dụ: "HANOI", "HOCHIMINH").
    /// </summary>
    public string? MerchantCity { get; set; }

    /// <summary>
    /// Toàn bộ các trường dữ liệu bổ sung trong Tag 62.
    /// </summary>
    public AdditionalDataOptions? AdditionalData { get; set; }

    /// <summary>
    /// Giá trị mã kiểm tra CRC16 (Tag 63) được trích xuất từ payload.
    /// </summary>
    public string? Crc { get; set; }

    /// <summary>
    /// Kết quả xác thực mã kiểm tra CRC16 (true nếu checksum hợp lệ).
    /// </summary>
    public bool IsValidCrc { get; set; }

    /// <summary>
    /// Bảng từ điển chứa tất cả các tag gốc thô đã được parse từ chuỗi EMVCo.
    /// </summary>
    public IReadOnlyDictionary<string, string> RawTags { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Toàn bộ chuỗi payload thô ban đầu.
    /// </summary>
    public string RawPayload { get; set; } = string.Empty;
}

