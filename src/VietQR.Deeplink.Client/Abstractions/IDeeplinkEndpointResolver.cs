namespace VietQR.Deeplink.Client;

/// <summary>
/// Giao diện phân giải địa chỉ domain cổng VietQR Deeplink (hỗ trợ Discovery và Caching).
/// </summary>
public interface IDeeplinkEndpointResolver
{
    /// <summary>
    /// Lấy địa chỉ domain máy chủ Deeplink đang hoạt động.
    /// </summary>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Domain máy chủ (ví dụ: https://deeplink.example.com).</returns>
    Task<string> ResolveBaseUrlAsync(CancellationToken ct = default);
}

