using System.Net;

namespace VietQR.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Luồng middleware của API: Swagger, CORS, security headers, HTTPS,
    /// static files, caching, rate limiter, health checks và controllers.
    /// </summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Swagger chỉ mở trong môi trường Development
        if (app.Environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    var isLoopback = context.Connection.RemoteIpAddress is null ||
                                     IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
                    var hasForwarded = context.Request.Headers.ContainsKey("X-Forwarded-Host") ||
                    context.Request.Headers.ContainsKey("X-Forwarded-For") ||
                    context.Request.Headers.ContainsKey("X-Original-Host") ||
                    context.Request.Headers.ContainsKey("X-Original-For");

                    if (!isLoopback || hasForwarded)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                }
                await next();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "VietQR Core API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseCors();

        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                ?? context.TraceIdentifier;
            context.Response.Headers.Append("X-Correlation-Id", correlationId);
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        if (!app.Environment.IsDevelopment() &&
            (!string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORT"]) ||
             !string.IsNullOrEmpty(app.Configuration["HTTPS_PORT"])))
        {
            app.UseHttpsRedirection();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                }
            }
        });

        var iconsDir = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "_content", "VietQR.Deeplink", "icons");
        if (!Directory.Exists(iconsDir))
        {
            iconsDir = Path.Combine(app.Environment.ContentRootPath, "..", "VietQR.Deeplink", "wwwroot", "icons");
        }

        if (Directory.Exists(iconsDir))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(iconsDir)),
                RequestPath = "/icons"
            });
        }

        // Phục vụ các file kiểm thử HTML 
        if (app.Environment.IsDevelopment())
        {
            var testsDirectoryPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "tests"));
            if (Directory.Exists(testsDirectoryPath))
            {
                var testsProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(testsDirectoryPath);
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = testsProvider,
                    OnPrepareResponse = ctx =>
                    {
                        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                        }
                    }
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = testsProvider,
                    RequestPath = "/tests",
                    OnPrepareResponse = ctx =>
                    {
                        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                        }
                    }
                });
            }
        }
        app.UseResponseCaching();
        app.UseRateLimiter();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapControllers();

#if HAS_VIETQR_DEEPLINK
        VietQR.Deeplink.DeeplinkHostExtensions.UseDeeplinkServer(app);
#endif

        return app;
    }
}
