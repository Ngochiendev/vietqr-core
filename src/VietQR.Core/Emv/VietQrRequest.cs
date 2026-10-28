namespace VietQR.Core;

/// <summary>
/// Yêu cầu sinh mã thanh toán VietQR chuẩn NAPAS247.
/// </summary>
/// <param name="Bin">Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số, ví dụ BankBin.Create("970422")).</param>
/// <param name="AccountNo">Số tài khoản, số thẻ hoặc tài khoản định danh/thu hộ (tối đa 25 ký tự).</param>
/// <param name="Amount">Số tiền thanh toán VNĐ (> 0, tùy chọn đối với mã QR tĩnh).</param>
/// <param name="Description">Nội dung chuyển khoản (tối đa 95 ký tự theo chuẩn EMVCo Tag 62).</param>
/// <param name="ServiceType">Loại dịch vụ chuyển tiền: Account (tài khoản - mặc định) hoặc Card (thẻ).</param>
public sealed record VietQrRequest(
    BankBin Bin,
    string AccountNo,
    long? Amount = null,
    string? Description = null,
    VietQrServiceType ServiceType = VietQrServiceType.Account);
