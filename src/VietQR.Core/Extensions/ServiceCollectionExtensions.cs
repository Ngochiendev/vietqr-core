using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VietQR.Core.Abstractions;
using VietQR.Core.Emv;
using VietQR.Core.Internal;
using VietQR.Core.Rendering;
using VietQR.Core.Rendering.Templates;

namespace VietQR.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký toàn bộ dịch vụ của VietQR.Core vào IServiceCollection.
    /// Dùng được cho ASP.NET Core, Worker Service, CLI application.
    /// </summary>
    public static IServiceCollection AddVietQrCore(
        this IServiceCollection services,
        Action<VietQrOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IQrGenerator, VietQr>();
        services.TryAddSingleton<IBankCatalog>(VietQR.Core.Banks.EmbeddedBankCatalog.Instance);
        services.TryAddSingleton<QrImageService>();
        services.TryAddSingleton<IQrImageRenderer, SkiaQrImageRenderer>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IQrTemplate, CompactTemplate>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IQrTemplate, QrOnlyTemplate>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IQrTemplate, PrintTemplate>());

        return services;
    }
}
