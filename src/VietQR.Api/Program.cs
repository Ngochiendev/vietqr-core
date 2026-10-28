using VietQR.Api.Extensions;
using VietQR.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services
    .AddVietQrCore()
    .AddApiInfrastructure(builder.Configuration)
    .AddApiPresentation(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();

app.Run();

public partial class Program { }
