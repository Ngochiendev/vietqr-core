namespace VietQR.Core;

/// <summary>
/// Cấu hình tùy chọn khi giải mã chuỗi VietQR.
/// </summary>
public sealed class VietQrParseOptions
{
    /// <summary>
    /// Bắt buộc kiểm tra mã kiểm tra CRC16 (Tag 63).
    /// Mặc định là true (chuỗi sai checksum sẽ trả về thất bại ngay lập tức).
    /// </summary>
    public bool StrictCrc { get; set; } = true;

    /// <summary>
    /// Giới hạn độ dài tối đa của chuỗi payload (mặc định 1024 ký tự) nhằm chống tấn công DoS/ReDoS qua camera hoặc webhook.
    /// </summary>
    public int MaxPayloadLength { get; set; } = 1024;

    /// <summary>
    /// Danh mục ngân hàng dùng để tự động tra cứu thông tin ngân hàng theo mã BIN.
    /// Nếu null, hệ thống sẽ sử dụng danh mục nhúng sẵn <see cref="EmbeddedBankCatalog.Instance"/>.
    /// </summary>
    public IBankCatalog? BankCatalog { get; set; }
}

