using VietQR.Core;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Extension method mở rộng trực tiếp cho đối tượng VietQr.
/// </summary>
public static class VietQrExtensions
{
    /// <summary>
    /// Lấy link mở app ngân hàng tự điền thanh toán từ đối tượng <see cref="VietQr"/>.
    /// </summary>
    /// <param name="qr">Đối tượng VietQr đã tạo.</param>
    /// <param name="client">Client Deeplink đã inject.</param>
    /// <param name="appCode">Mã app hoặc BIN ngân hàng (ví dụ: MB, TPB, 970422).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Link mở app hoặc <c>null</c> nếu không thành công.</returns>
    public static Task<DeeplinkIntentResult?> ToDeeplinkUrlAsync(
        this VietQr qr,
        IVietQrDeeplinkClient client,
        string appCode,
        CancellationToken ct = default)
    {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (client is null) throw new ArgumentNullException(nameof(client));

        return client.CreateIntentAsync(qr, appCode, ct);
    }
}
