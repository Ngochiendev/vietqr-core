namespace VietQR.Core;

/// <summary>
/// Tiện ích đóng gói chuỗi payload EMVCo VietQR chuẩn NAPAS247.
/// Hỗ trợ đầy đủ 4 trường hợp chuẩn theo tài liệu NAPAS (Mục 6.1):
/// 1. QR tĩnh đến tài khoản (6.1.1)
/// 2. QR tĩnh đến thẻ (6.1.2)
/// 3. QR động đến tài khoản (6.1.3)
/// 4. QR động đến thẻ (6.1.4)
/// </summary>
public static class VietQrPayload
{
    /// <summary>
    /// Tạo chuỗi mã VietQR tổng quát (chuẩn NAPAS247 / EMVCo) với loại QR và loại dịch vụ tùy chọn.
    /// </summary>
    /// <param name="bin">Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số).</param>
    /// <param name="accountNo">Số tài khoản, số thẻ hoặc mã tài khoản định danh (tối đa 25 ký tự).</param>
    /// <param name="qrType">Loại mã QR: Static (tĩnh) hoặc Dynamic (động).</param>
    /// <param name="serviceType">Loại dịch vụ: Account (tài khoản - mặc định) hoặc Card (thẻ).</param>
    /// <param name="amount">Số tiền thanh toán VNĐ (> 0 khi qrType là Dynamic).</param>
    /// <param name="description">Nội dung thanh toán (tùy chọn / optional, tự động loại bỏ dấu tiếng Việt).</param>
    /// <param name="billNumber">Số hóa đơn / mã tham chiếu Tag 62.01 (Tùy chọn / Optional - tối đa 25 ký tự, có thể bỏ qua hoặc để null).</param>
    /// <returns>Chuỗi mã QR định dạng EMVCo kèm mã kiểm tra CRC16 hợp lệ.</returns>
    public static string Create(
        BankBin bin,
        string accountNo,
        VietQrType qrType,
        VietQrServiceType serviceType = VietQrServiceType.Account,
        long? amount = null,
        string? description = null,
        string? billNumber = null)
    {
        if (qrType == VietQrType.Dynamic && (!amount.HasValue || amount.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Mã QR động (VietQrType.Dynamic) bắt buộc phải có số tiền lớn hơn 0.");
        }

        var builder = new VietQr()
            .SetBeneficiaryOrganization(bin.Value, accountNo, serviceType)
            .SetPointOfInitiationMethod(qrType == VietQrType.Dynamic);

        if (amount.HasValue && amount.Value > 0)
        {
            builder.SetTransactionAmount(amount.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(description) || !string.IsNullOrWhiteSpace(billNumber))
        {
            builder.SetAdditionalData(purpose: description, billNumber: billNumber);
        }

        return builder.Build();
    }

    /// <summary>
    /// Tạo chuỗi mã VietQR tổng quát (tự động nhận diện Dynamic nếu amount > 0, ngược lại là Static).
    /// </summary>
    public static string Create(
        BankBin bin,
        string accountNo,
        long? amount = null,
        string? description = null,
        VietQrServiceType serviceType = VietQrServiceType.Account,
        string? billNumber = null)
    {
        var qrType = (amount.HasValue && amount.Value > 0) ? VietQrType.Dynamic : VietQrType.Static;
        return Create(bin, accountNo, qrType, serviceType, amount, description, billNumber);
    }

    /// <summary>
    /// Tạo mã VietQR Tĩnh (Static QR - Tag 01 = "11", Mục 6.1.1 &amp; 6.1.2 NAPAS).
    /// Khách hàng tự nhập số tiền khi quét mã trên ứng dụng ngân hàng.
    /// </summary>
    /// <param name="bin">Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số).</param>
    /// <param name="accountNo">Số tài khoản hoặc số thẻ.</param>
    /// <param name="serviceType">Loại dịch vụ: Account (chuyển đến tài khoản - mặc định) hoặc Card (chuyển đến thẻ).</param>
    /// <param name="description">Nội dung chuyển khoản gợi ý (tùy chọn, tự động khử dấu).</param>
    /// <returns>Chuỗi mã QR chuẩn EMVCo phương thức tĩnh.</returns>
    public static string CreateStatic(
        BankBin bin,
        string accountNo,
        VietQrServiceType serviceType = VietQrServiceType.Account,
        string? description = null)
    {
        return Create(bin, accountNo, VietQrType.Static, serviceType, amount: null, description: description);
    }

    /// <summary>
    /// Tạo mã VietQR Động (Dynamic QR - Tag 01 = "12", Mục 6.1.3 &amp; 6.1.4 NAPAS).
    /// Đã khóa cố định số tiền thanh toán (> 0) và hỗ trợ số hóa đơn (billNumber).
    /// </summary>
    /// <param name="bin">Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số).</param>
    /// <param name="accountNo">Số tài khoản hoặc số thẻ.</param>
    /// <param name="amount">Số tiền thanh toán VNĐ (> 0).</param>
    /// <param name="serviceType">Loại dịch vụ: Account (chuyển đến tài khoản - mặc định) hoặc Card (chuyển đến thẻ).</param>
    /// <param name="description">Nội dung thanh toán đơn hàng (tùy chọn / optional, tự động khử dấu).</param>
    /// <param name="billNumber">Số hóa đơn / mã đơn hàng Tag 62.01 (Tùy chọn / Optional - có thể bỏ qua hoặc để null).</param>
    /// <returns>Chuỗi mã QR chuẩn EMVCo phương thức động.</returns>
    public static string CreateDynamic(
        BankBin bin,
        string accountNo,
        long amount,
        VietQrServiceType serviceType = VietQrServiceType.Account,
        string? description = null,
        string? billNumber = null)
    {
        return Create(bin, accountNo, VietQrType.Dynamic, serviceType, amount: amount, description: description, billNumber: billNumber);
    }

    /// <summary>Tạo mã VietQR tĩnh đến Tài khoản (Mục 6.1.1 NAPAS).</summary>
    public static string CreateStaticAccount(BankBin bin, string accountNo, string? description = null) =>
        CreateStatic(bin, accountNo, VietQrServiceType.Account, description);

    /// <summary>Tạo mã VietQR tĩnh đến Thẻ (Mục 6.1.2 NAPAS).</summary>
    public static string CreateStaticCard(BankBin bin, string cardNumber, string? description = null) =>
        CreateStatic(bin, cardNumber, VietQrServiceType.Card, description);

    /// <summary>Tạo mã VietQR động đến Tài khoản (Mục 6.1.3 NAPAS).</summary>
    public static string CreateDynamicAccount(BankBin bin, string accountNo, long amount, string? description = null, string? billNumber = null) =>
        CreateDynamic(bin, accountNo, amount, VietQrServiceType.Account, description, billNumber);

    /// <summary>Tạo mã VietQR động đến Thẻ (Mục 6.1.4 NAPAS).</summary>
    public static string CreateDynamicCard(BankBin bin, string cardNumber, long amount, string? description = null, string? billNumber = null) =>
        CreateDynamic(bin, cardNumber, amount, VietQrServiceType.Card, description, billNumber);
}
