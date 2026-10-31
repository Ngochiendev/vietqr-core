using VietQR.Core;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Client gọi cổng VietQR Deeplink để lấy link mở app ngân hàng tự điền thanh toán.
/// </summary>
public interface IVietQrDeeplinkClient
{
    /// <summary>
    /// Kiểm tra client đã sẵn sàng hoạt động hay chưa.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Tạo link mở app ngân hàng từ mã app và chuỗi VietQR EMVCo.
    /// </summary>
    /// <param name="appCode">Mã app hoặc BIN ngân hàng (ví dụ: MB, TPB, 970422).</param>
    /// <param name="qrContent">Chuỗi mã QR chuẩn EMVCo/NAPAS247.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Link mở app kèm thời hạn, hoặc <c>null</c> nếu không thành công.</returns>
    Task<DeeplinkIntentResult?> CreateIntentAsync(string appCode, string qrContent, CancellationToken ct = default);

    /// <summary>
    /// Tạo link mở app ngân hàng trực tiếp từ đối tượng <see cref="VietQr"/>.
    /// </summary>
    /// <param name="qr">Đối tượng VietQr đã tạo thông tin thanh toán.</param>
    /// <param name="appCode">Mã app hoặc BIN ngân hàng (ví dụ: MB, TPB, 970422).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Link mở app kèm thời hạn, hoặc <c>null</c> nếu không thành công.</returns>
    Task<DeeplinkIntentResult?> CreateIntentAsync(VietQr qr, string appCode, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách các ứng dụng ngân hàng và ví điện tử được cổng Deeplink hỗ trợ.
    /// </summary>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách các ứng dụng được hỗ trợ, hoặc danh sách rỗng nếu không thể kết nối.</returns>
    Task<IReadOnlyList<DeeplinkBankAppInfo>> GetSupportedAppsAsync(CancellationToken ct = default);
}
