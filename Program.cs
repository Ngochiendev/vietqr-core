using Microsoft.OpenApi.Models;
using System.Reflection;
using vietqr_core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VietQR Core API (NAPAS247 Microservice)",
        Version = "v1.0",
        Description = """
        ### Dịch vụ vi mô tạo chuỗi & ảnh mã thanh toán VietQR chuẩn NAPAS247

        Dự án `vietqr-core` cung cấp bộ API hiệu năng cao phục vụ:
        - **Sinh chuỗi EMVCo:** Chuẩn NAPAS247 (chuyển nhanh qua tài khoản `QRIBFTTA` hoặc thẻ `QRIBFTTC`).
        - **Render ảnh đồ họa:** Sử dụng SkiaSharp 4.x với thuật toán nội suy `Mitchell` và nhận diện tự động biên thực tế logo `GetContentBounds`.
        - **Hỗ trợ 2 mẫu template:**
          - `compact`: Mẫu thẻ ảnh đầy đủ (Header VietQR + Khung thẻ + QR Code + Footer Napas247 & Ngân hàng).
          - `qr_only`: Mẫu tinh gọn chỉ gồm mã QR vuông 500x500px kèm logo VietQR ở tâm.
        - **Vận hành độc lập:** Tự động đồng bộ và lưu trữ danh mục ngân hàng vào file đĩa cứng `banks.json`, tích hợp bộ đệm đa tầng RAM/Disk.
        """
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient("VietQrClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; VietQrCore/1.0)");
});

builder.Services.AddSingleton<BankSyncBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BankSyncBackgroundService>());
builder.Services.AddSingleton<QrImageService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VietQR Core API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.MapControllers();

app.Run();
