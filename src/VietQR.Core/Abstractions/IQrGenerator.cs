using VietQR.Core.Emv;

namespace VietQR.Core.Abstractions;

/// <summary>Dịch vụ sinh chuỗi mã thanh toán EMVCo/NAPAS247 chạy offline 100%, không phụ thuộc I/O.</summary>
public interface IQrGenerator
{
    /// <summary>Sinh chuỗi mã thanh toán VietQR chuẩn EMVCo.</summary>
    /// <param name="request">Yêu cầu tạo mã (chứa mã BIN ngân hàng, số tài khoản, số tiền, nội dung).</param>
    /// <returns>Chuỗi mã QR EMVCo hoàn chỉnh có kèm mã kiểm tra CRC16 (VD: 0002010102123854...).</returns>
    string Generate(VietQrRequest request);
}
