using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Bộ phân giải domain máy chủ VietQR Deeplink, tự động khám phá và lưu bộ nhớ đệm (In-Memory Cache).
/// </summary>
public sealed class DeeplinkEndpointResolver : IDeeplinkEndpointResolver
{
    private readonly HttpClient http;
    private readonly VietQrDeeplinkOptions options;
    private readonly ILogger<DeeplinkEndpointResolver> logger;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private string? cachedGateway;
    private DateTime cacheExpiresAt = DateTime.MinValue;

    public DeeplinkEndpointResolver(
        HttpClient http,
        IOptions<VietQrDeeplinkOptions> options,
        ILogger<DeeplinkEndpointResolver>? logger = null)
    {
        this.http = http ?? throw new ArgumentNullException(nameof(http));
        this.options = options?.Value ?? new VietQrDeeplinkOptions();
        this.logger = logger ?? NullLogger<DeeplinkEndpointResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<string> ResolveBaseUrlAsync(CancellationToken ct = default)
    {
        // Nếu người dùng cấu hình BaseUrl cụ thể -> dùng luôn, không cần discovery
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return options.BaseUrl.TrimEnd('/');
        }

        // Nếu tắt tính năng Discovery -> dùng DefaultGateway
        if (!options.EnableDiscovery)
        {
            return options.DefaultGateway.TrimEnd('/');
        }

        // Kiểm tra In-Memory Cache
        if (!string.IsNullOrWhiteSpace(cachedGateway) && DateTime.UtcNow < cacheExpiresAt)
        {
            return cachedGateway;
        }

        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-checked locking
            if (!string.IsNullOrWhiteSpace(cachedGateway) && DateTime.UtcNow < cacheExpiresAt)
            {
                return cachedGateway;
            }

            var discovered = await FetchDiscoveryGatewayAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(discovered))
            {
                cachedGateway = discovered.TrimEnd('/');
                cacheExpiresAt = DateTime.UtcNow.AddHours(Math.Max(1, options.DiscoveryCacheHours));
                logger.LogInformation("Khám phá thành công domain VietQR Deeplink: {Gateway}", cachedGateway);
                return cachedGateway;
            }

            // Fallback khi Discovery không trả về gateway hợp lệ
            cachedGateway = options.DefaultGateway.TrimEnd('/');
            cacheExpiresAt = DateTime.UtcNow.AddMinutes(15);
            return cachedGateway;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<string?> FetchDiscoveryGatewayAsync(CancellationToken ct)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var doc = await http.GetFromJsonAsync<DiscoveryDocument>(options.DiscoveryUrl, linkedCts.Token).ConfigureAwait(false);
            return doc?.Gateway;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể kết nối Discovery Endpoint tại {DiscoveryUrl}. Tự động fallback về {DefaultGateway}.",
                options.DiscoveryUrl, options.DefaultGateway);
            return null;
        }
    }

    private sealed class DiscoveryDocument
    {
        [JsonPropertyName("gateway")]
        public string? Gateway { get; set; }

        [JsonPropertyName("fallbackGateways")]
        public List<string>? FallbackGateways { get; set; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }
    }
}

