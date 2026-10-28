namespace VietQR.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "VietQR Core API v1");
            c.RoutePrefix = "swagger";
        });

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

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseResponseCaching();
        app.UseRateLimiter();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapControllers();

        return app;
    }
}
