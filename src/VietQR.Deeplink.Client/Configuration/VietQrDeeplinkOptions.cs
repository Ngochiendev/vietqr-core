namespace VietQR.Deeplink.Client;

/// <summary>
/// Cấu hình kết nối cổng VietQR Deeplink.
/// </summary>
public sealed class VietQrDeeplinkOptions
{
    /// <summary>
    /// Section name trong appsettings.json. Mặc định là "Deeplink".
    /// </summary>
    public const string SectionName = "Deeplink";

    /// <summary>
    /// Domain cổng deeplink cụ thể. Nếu để trống, SDK sẽ tự động khám phá qua Discovery Endpoint.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// URL file cấu hình Discovery tĩnh (mặc định từ GitHub repo vietqr-core).
    /// </summary>
    public string DiscoveryUrl { get; set; } =
        "https://raw.githubusercontent.com/Ngochiendev/vietqr-core/main/vietqr-discovery.json";

    /// <summary>
    /// Domain dự phòng mặc định khi không kết nối được Discovery.
    /// </summary>
    public string DefaultGateway { get; set; } = "https://vietqr.hiendev.io.vn";

    /// <summary>
    /// Bật hoặc tắt tính năng tự động khám phá domain (mặc định bật).
    /// </summary>
    public bool EnableDiscovery { get; set; } = true;

    /// <summary>
    /// Thời gian lưu cache domain đã khám phá (giờ), mặc định 12 tiếng.
    /// </summary>
    public int DiscoveryCacheHours { get; set; } = 12;

    /// <summary>
    /// Thời gian chờ khi gọi API tạo link (giây). Mặc định 10s.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Kiểm tra client đã sẵn sàng hoạt động hay chưa (có BaseUrl, bật Discovery hoặc có DefaultGateway).
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl) ||
        EnableDiscovery ||
        !string.IsNullOrWhiteSpace(DefaultGateway);
}
