using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VietQR.Core;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Client kết nối cổng VietQR Deeplink để lấy link mở app ngân hàng tự điền thanh toán.
/// </summary>
public sealed class VietQrDeeplinkClient : IVietQrDeeplinkClient
{
    private readonly HttpClient http;
    private readonly VietQrDeeplinkOptions options;
    private readonly IDeeplinkEndpointResolver endpointResolver;
    private readonly ILogger<VietQrDeeplinkClient> logger;

    [ActivatorUtilitiesConstructor]
    public VietQrDeeplinkClient(
        HttpClient http,
        IOptions<VietQrDeeplinkOptions> options,
        IDeeplinkEndpointResolver endpointResolver,
        ILogger<VietQrDeeplinkClient>? logger = null)
    {
        this.http = http ?? throw new ArgumentNullException(nameof(http));
        this.options = options?.Value ?? new VietQrDeeplinkOptions();
        this.endpointResolver = endpointResolver ?? throw new ArgumentNullException(nameof(endpointResolver));
        this.logger = logger ?? NullLogger<VietQrDeeplinkClient>.Instance;
    }

    public VietQrDeeplinkClient(
        HttpClient http,
        IOptions<VietQrDeeplinkOptions> options,
        ILogger<VietQrDeeplinkClient>? logger = null)
        : this(http, options, new DeeplinkEndpointResolver(http, options), logger)
    {
    }

    /// <inheritdoc />
    public bool IsConfigured => options.IsConfigured;

    /// <inheritdoc />
    public Task<DeeplinkIntentResult?> CreateIntentAsync(VietQr qr, string appCode, CancellationToken ct = default)
    {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        var qrContent = qr.Build();
        return CreateIntentAsync(appCode, qrContent, ct);
    }

    /// <inheritdoc />
    public async Task<DeeplinkIntentResult?> CreateIntentAsync(string appCode, string qrContent, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Chưa cấu hình dịch vụ VietQR Deeplink.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(appCode) || string.IsNullOrWhiteSpace(qrContent))
        {
            return null;
        }

        var trimmedQr = qrContent.Trim();
        if (!VietQr.VerifyCRC(trimmedQr))
        {
            logger.LogWarning("Chuỗi VietQR không hợp lệ hoặc sai mã kiểm tra CRC16.");
            return null;
        }

        try
        {
            var baseUrl = await endpointResolver.ResolveBaseUrlAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                logger.LogWarning("Không thể phân giải domain máy chủ VietQR Deeplink.");
                return null;
            }

            var endpoint = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "v1/intents");
            var payload = new DeeplinkIntentRequest(appCode.Trim(), trimmedQr);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Cổng VietQR Deeplink trả về mã lỗi HTTP {StatusCode}.", response.StatusCode);
                return null;
            }

            var envelope = await response.Content.ReadFromJsonAsync<GatewayEnvelope>(cancellationToken: ct).ConfigureAwait(false);
            return envelope?.Data;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi kết nối tới cổng VietQR Deeplink.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeeplinkBankAppInfo>> GetSupportedAppsAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Chưa cấu hình dịch vụ VietQR Deeplink.");
            return Array.Empty<DeeplinkBankAppInfo>();
        }

        try
        {
            var baseUrl = await endpointResolver.ResolveBaseUrlAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                logger.LogWarning("Không thể phân giải domain máy chủ VietQR Deeplink.");
                return Array.Empty<DeeplinkBankAppInfo>();
            }

            var endpoint = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "v1/apps");
            using var response = await http.GetAsync(endpoint, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Cổng VietQR Deeplink trả về mã lỗi HTTP {StatusCode} khi lấy danh sách ứng dụng.", response.StatusCode);
                return Array.Empty<DeeplinkBankAppInfo>();
            }

            var envelope = await response.Content.ReadFromJsonAsync<AppsGatewayEnvelope>(cancellationToken: ct).ConfigureAwait(false);
            return envelope?.Data ?? (IReadOnlyList<DeeplinkBankAppInfo>)Array.Empty<DeeplinkBankAppInfo>();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi kết nối tới cổng VietQR Deeplink khi lấy danh sách ứng dụng.");
            return Array.Empty<DeeplinkBankAppInfo>();
        }
    }

    private sealed class GatewayEnvelope
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("data")]
        public DeeplinkIntentResult? Data { get; set; }
    }

    private sealed class AppsGatewayEnvelope
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("data")]
        public List<DeeplinkBankAppInfo>? Data { get; set; }
    }
}
