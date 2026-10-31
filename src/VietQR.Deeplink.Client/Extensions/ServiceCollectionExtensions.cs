using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace VietQR.Deeplink.Client;

/// <summary>
/// Extension đăng ký dịch vụ VietQR Deeplink vào IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký client VietQR Deeplink với cấu hình mặc định (Zero-config, tự động Discovery domain).
    /// </summary>
    /// <param name="services">DI ServiceCollection.</param>
    public static IServiceCollection AddVietQrDeeplink(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddOptions<VietQrDeeplinkOptions>();
        RegisterClientCore(services);
        return services;
    }

    /// <summary>
    /// Đăng ký client VietQR Deeplink bằng Action cấu hình options.
    /// </summary>
    /// <param name="services">DI ServiceCollection.</param>
    /// <param name="configure">Hàm cấu hình VietQrDeeplinkOptions.</param>
    public static IServiceCollection AddVietQrDeeplink(
        this IServiceCollection services,
        Action<VietQrDeeplinkOptions> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.Configure(configure);
        RegisterClientCore(services);
        return services;
    }

    /// <summary>
    /// Đăng ký client VietQR Deeplink từ IConfiguration section (mặc định "Deeplink").
    /// </summary>
    /// <param name="services">DI ServiceCollection.</param>
    /// <param name="configuration">Section chứa cấu hình Deeplink.</param>
    public static IServiceCollection AddVietQrDeeplink(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<VietQrDeeplinkOptions>(configuration);
        RegisterClientCore(services);
        return services;
    }

    private static void RegisterClientCore(IServiceCollection services)
    {
        services.AddHttpClient<IDeeplinkEndpointResolver, DeeplinkEndpointResolver>();

        services.AddHttpClient<IVietQrDeeplinkClient, VietQrDeeplinkClient>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<VietQrDeeplinkOptions>>().Value;
            if (opt.TimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
            }
        });
    }
}
