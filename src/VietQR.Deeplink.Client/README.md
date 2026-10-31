# VietQR.Deeplink.Client

[![NuGet](https://img.shields.io/nuget/v/VietQR.Deeplink.Client?style=flat-square&color=004880)](https://www.nuget.org/packages/VietQR.Deeplink.Client)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VietQR.Deeplink.Client?style=flat-square&color=0078D4)](https://www.nuget.org/packages/VietQR.Deeplink.Client)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://github.com/Ngochiendev/vietqr-core)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE)

Thư viện client C# (.NET) kết nối dịch vụ VietQR Deeplink để lấy link mở ứng dụng ngân hàng khi thanh toán bằng mã VietQR, hỗ trợ Dependency Injection trong ASP.NET Core và phương thức mở rộng cho [`VietQR.Core`](https://www.nuget.org/packages/VietQR.Core).

---

## ⚡ Tính năng

- **Mở rộng VietQR.Core:** Tạo mã thanh toán và lấy link mở app ngân hàng nhanh chóng.
- **Dependency Injection:** Đăng ký qua `services.AddVietQrDeeplink()` trong ASP.NET Core hoặc Generic Host.
- **Tự động nhận diện Endpoint:** Tự động cập nhật domain máy chủ Deeplink qua Discovery Endpoint (có in-memory cache).
- **Hỗ trợ đa nền tảng .NET:** Chạy tốt trên .NET 6.0, .NET 8.0, .NET 9.0+.

---

## 📦 Cài đặt

```bash
dotnet add package VietQR.Core
dotnet add package VietQR.Deeplink.Client
```

## Hướng dẫn sử dụng

### 1. Đăng ký dịch vụ trong `Program.cs`

```csharp
using VietQR.Deeplink.Client;

// Cách 1: Sử dụng cấu hình mặc định (Zero-config, chạy ngay không cần tham số)
builder.Services.AddVietQrDeeplink();

// Cách 2: Tùy biến domain máy chủ hoặc timeout nếu tự host
builder.Services.AddVietQrDeeplink(options =>
{
    options.BaseUrl = "https://deeplink.example.com";
    options.TimeoutSeconds = 10;
});
```

Cấu hình mẫu trong `appsettings.json` (tùy chọn, không bắt buộc):

```json
{
    "Deeplink": {
        "BaseUrl": "https://deeplink.example.com",
        "TimeoutSeconds": 10
    }
}
```

### 2. Sử dụng trong Service hoặc Controller

```csharp
using VietQR.Core;
using VietQR.Deeplink.Client;

public class CheckoutController : ControllerBase
{
    private readonly IVietQrDeeplinkClient _deeplinkClient;

    public CheckoutController(IVietQrDeeplinkClient deeplinkClient)
    {
        _deeplinkClient = deeplinkClient;
    }

    [HttpPost("pay")]
    public async Task<IActionResult> CreatePayment()
    {
        // 1. Tạo chuỗi QR thanh toán chuẩn Napas247 bằng VietQR.Core
        var qr = new VietQr()
            .SetBeneficiaryOrganization("970422", "113366668888", VietQrServiceType.Account)
            .SetTransactionAmount("50000")
            .SetAdditionalData(purpose: "Thanh toan don hang #1024");

        // 2. Lấy link ngắn hạn mở app ngân hàng MB
        var intent = await qr.ToDeeplinkUrlAsync(_deeplinkClient, "MB");

        if (intent == null)
        {
            return BadRequest("Không thể tạo link mở app ngân hàng.");
        }

        // intent.Url: "https://deeplink.example.com/p/x8K2mN9..."
        // intent.ExpiresInSeconds: 60
        return Ok(new { paymentUrl = intent.Url, expiresIn = intent.ExpiresInSeconds });
    }
}
```

## Giấy phép

Phát hành theo giấy phép [MIT License](LICENSE).
