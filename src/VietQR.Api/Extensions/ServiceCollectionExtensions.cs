using System.Reflection;
using Microsoft.OpenApi.Models;
using VietQR.Api.Controllers;
using VietQR.Deeplink.Client;

namespace VietQR.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký health checks, response caching và rate limiter cho API.
    /// </summary>
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHealthChecks();
        services.AddResponseCaching();
        services.AddHttpContextAccessor();
        services.AddTransient<ForwardedIpHandler>();

        services.AddVietQrDeeplink(config.GetSection(VietQrDeeplinkOptions.SectionName));
        services.ConfigureHttpClientDefaults(b => b.AddHttpMessageHandler<ForwardedIpHandler>());

#if HAS_VIETQR_DEEPLINK
        VietQR.Deeplink.DeeplinkHostExtensions.AddDeeplinkServer(services, config);
#endif

        return services;
    }

    private static string ResolveClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    internal sealed class ForwardedIpHandler(IHttpContextAccessor accessor) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ctx = accessor.HttpContext;
            if (ctx is not null)
            {
                if (!request.Headers.Contains("X-Forwarded-For"))
                {
                    var ip = ResolveClientIp(ctx);
                    if (!string.IsNullOrWhiteSpace(ip) && ip != "unknown")
                    {
                        request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
                    }
                }

                if (!request.Headers.Contains("X-Forwarded-Host"))
                {
                    var host = ctx.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(host) && ctx.Request.Host.HasValue)
                    {
                        host = ctx.Request.Host.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", host);
                    }
                }

                if (!request.Headers.Contains("X-Forwarded-Proto"))
                {
                    var proto = ctx.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? ctx.Request.Scheme;
                    if (!string.IsNullOrWhiteSpace(proto))
                    {
                        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", proto);
                    }
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Đăng ký controllers, Swagger và CORS cho API.
    /// </summary>
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddControllers();
        services.AddTransient<QrCodesController>();
        services.AddEndpointsApiExplorer();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("generate", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    ResolveClientIp(httpContext),
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromSeconds(1),
                        QueueLimit = 50,
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                    }));
        });

        services.AddSwaggerGen(c =>
        {
            c.OperationFilter<DecodeOperationFilter>();

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VietQR Core API (NAPAS247 / EMVCo)",
                Version = "v1.0",
                Description = @"### API tạo chuỗi & render ảnh mã thanh toán VietQR chuẩn NAPAS247 & EMVCo

Dự án `vietqr-core` cung cấp bộ API hiệu năng cao phục vụ:
- **Sinh chuỗi EMVCo:** Chuẩn NAPAS247 (chuyển nhanh qua tài khoản `QRIBFTTA` hoặc thẻ `QRIBFTTC`).
- **Render ảnh đồ họa:** Sử dụng SkiaSharp 4.x hỗ trợ PNG, WebP (nhẹ hơn PNG ~40%) và SVG (vector, nét hơn).
- **Xuất ảnh trực tiếp:** Endpoint `/image` trả thẳng stream ảnh binary dùng được ngay trong thẻ `<img>`.
- **Giải mã QR từ ảnh (Decode):** Đọc mã QR từ ảnh tải lên hoặc Base64 và bóc tách thông tin giao dịch.
- **Phân tích chuỗi EMVCo (Parse):** Bóc tách chi tiết từng Tag TLV của chuỗi mã QR.
- **Hỗ trợ 3 mẫu template:**
  - `compact`: Mẫu thẻ ảnh đầy đủ (Header VietQR + Khung thẻ + QR Code + Footer Napas247 & Ngân hàng).
  - `qr_only`: Mẫu tinh gọn chỉ gồm mã QR vuông 500x500px kèm logo VietQR ở tâm.
  - `print`: Mẫu in ấn mở rộng hiển thị thông tin số tiền (VNĐ), nội dung chuyển khoản, tên và số tài khoản in đậm."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            var coreXmlPath = Path.Combine(AppContext.BaseDirectory, "VietQR.Core.xml");
            if (File.Exists(coreXmlPath))
            {
                c.IncludeXmlComments(coreXmlPath);
            }
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("X-Correlation-Id", "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset");
            });
        });

        return services;
    }
}
