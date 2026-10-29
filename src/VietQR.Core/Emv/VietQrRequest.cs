namespace VietQR.Core;

/// <summary>
/// Yêu cầu sinh mã thanh toán VietQR chuẩn NAPAS247.
/// </summary>
/// <param name="Bin">Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số, ví dụ BankBin.Create("970422")).</param>
/// <param name="AccountNo">Số tài khoản, số thẻ hoặc tài khoản định danh/thu hộ (tối đa 25 ký tự).</param>
/// <param name="Amount">Số tiền thanh toán VNĐ (> 0, bắt buộc đối với mã QR động).</param>
/// <param name="Description">Nội dung chuyển khoản (tối đa 95 ký tự theo chuẩn EMVCo Tag 62, Tùy chọn / Optional).</param>
/// <param name="ServiceType">Loại dịch vụ chuyển tiền: Account (tài khoản - mặc định) hoặc Card (thẻ).</param>
/// <param name="BillNumber">Số hóa đơn / mã đơn hàng (Tag 62.01, tối đa 25 ký tự, Tùy chọn / Optional - có thể bỏ qua hoặc để null).</param>
/// <param name="QrType">Loại mã QR: Static (tĩnh) hoặc Dynamic (động). Mặc định tự suy ra theo Amount.</param>
public sealed record VietQrRequest(
    BankBin Bin,
    string AccountNo,
    long? Amount = null,
    string? Description = null,
    VietQrServiceType ServiceType = VietQrServiceType.Account,
    string? BillNumber = null,
    VietQrType? QrType = null)
{
    /// <summary>
    /// Loại QR hiệu lực thực tế (nếu không set thì có số tiền > 0 sẽ là Dynamic, ngược lại là Static).
    /// </summary>
    public VietQrType EffectiveQrType =>
        QrType ?? ((Amount.HasValue && Amount.Value > 0) ? VietQrType.Dynamic : VietQrType.Static);

    /// <summary>Khởi tạo yêu cầu QR Tĩnh (Mục 6.1.1 &amp; 6.1.2 NAPAS).</summary>
    public static VietQrRequest CreateStatic(
        BankBin bin,
        string accountNo,
        VietQrServiceType serviceType = VietQrServiceType.Account,
        string? description = null) =>
        new(bin, accountNo, Amount: null, Description: description, ServiceType: serviceType, QrType: VietQrType.Static);

    /// <summary>Khởi tạo yêu cầu QR Động (Mục 6.1.3 &amp; 6.1.4 NAPAS).</summary>
    public static VietQrRequest CreateDynamic(
        BankBin bin,
        string accountNo,
        long amount,
        VietQrServiceType serviceType = VietQrServiceType.Account,
        string? description = null,
        string? billNumber = null) =>
        new(bin, accountNo, Amount: amount, Description: description, ServiceType: serviceType, BillNumber: billNumber, QrType: VietQrType.Dynamic);

    /// <summary>Khởi tạo yêu cầu QR tĩnh đến Tài khoản (6.1.1 NAPAS).</summary>
    public static VietQrRequest CreateStaticAccount(BankBin bin, string accountNo, string? description = null) =>
        CreateStatic(bin, accountNo, VietQrServiceType.Account, description);

    /// <summary>Khởi tạo yêu cầu QR tĩnh đến Thẻ (6.1.2 NAPAS).</summary>
    public static VietQrRequest CreateStaticCard(BankBin bin, string cardNumber, string? description = null) =>
        CreateStatic(bin, cardNumber, VietQrServiceType.Card, description);

    /// <summary>Khởi tạo yêu cầu QR động đến Tài khoản (6.1.3 NAPAS).</summary>
    public static VietQrRequest CreateDynamicAccount(BankBin bin, string accountNo, long amount, string? description = null, string? billNumber = null) =>
        CreateDynamic(bin, accountNo, amount, VietQrServiceType.Account, description, billNumber);

    /// <summary>Khởi tạo yêu cầu QR động đến Thẻ (6.1.4 NAPAS).</summary>
    public static VietQrRequest CreateDynamicCard(BankBin bin, string cardNumber, long amount, string? description = null, string? billNumber = null) =>
        CreateDynamic(bin, cardNumber, amount, VietQrServiceType.Card, description, billNumber);
}
