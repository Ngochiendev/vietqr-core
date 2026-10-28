using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using VietQR.Api.Controllers;

namespace VietQR.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHealthChecks();
        services.AddResponseCaching();

        return services;
    }

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
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
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
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VietQR Core API (NAPAS247 Microservice)",
                Version = "v1.0",
                Description = @"### Dịch vụ vi mô tạo chuỗi & ảnh mã thanh toán VietQR chuẩn NAPAS247

Dự án `vietqr-core` cung cấp bộ API hiệu năng cao phục vụ:
- **Sinh chuỗi EMVCo:** Chuẩn NAPAS247 (chuyển nhanh qua tài khoản `QRIBFTTA` hoặc thẻ `QRIBFTTC`).
- **Render ảnh đồ họa:** Sử dụng SkiaSharp 4.x với thuật toán nội suy `Mitchell` và nhận diện tự động biên thực tế logo `GetContentBounds`.
- **Hỗ trợ 3 mẫu template:**
  - `compact`: Mẫu thẻ ảnh đầy đủ (Header VietQR + Khung thẻ + QR Code + Footer Napas247 & Ngân hàng).
  - `qr_only`: Mẫu tinh gọn chỉ gồm mã QR vuông 500x500px kèm logo VietQR ở tâm.
  - `print`: Mẫu in ấn mở rộng hiển thị thông tin số tiền (VNĐ), nội dung chuyển khoản, tên và số tài khoản in đậm.
- **Vận hành Zero-I/O:** Nhúng sẵn 76+ ngân hàng thành viên NAPAS trực tiếp vào assembly, tra cứu siêu tốc từ bộ nhớ RAM."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
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
