# VietQR.Deeplink.Client

[![NuGet](https://img.shields.io/nuget/v/VietQR.Deeplink.Client?style=flat-square&color=004880)](https://www.nuget.org/packages/VietQR.Deeplink.Client)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VietQR.Deeplink.Client?style=flat-square&color=0078D4)](https://www.nuget.org/packages/VietQR.Deeplink.Client)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://github.com/Ngochiendev/vietqr-core)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE)

Thư viện client C# (.NET) kết nối dịch vụ VietQR Deeplink để lấy link mở ứng dụng ngân hàng khi thanh toán bằng mã VietQR, hỗ trợ Dependency Injection trong ASP.NET Core và phương thức mở rộng cho [`VietQR.Core`](https://www.nuget.org/packages/VietQR.Core).

---

## ⚡ Tính năng

- **Mở rộng VietQR.Core:** Tạo mã thanh toán và lấy link mở app ngân hàng nhanh chóng chỉ với 1 dòng lệnh.
- **Tra cứu danh sách App tự động:** Cung cấp hàm `GetSupportedAppsAsync()` và API công khai để lấy danh sách 43+ ngân hàng & ví điện tử (kèm logo, mã BIN, cờ `SupportsAutofill`).
- **Truyền mã linh hoạt:** Chấp nhận `AppCode` (`"MB"`), `ShortName` (`"MBBank"`), mã `BIN` 6 số (`"970422"`) hoặc `Alias` (không phân biệt hoa thường).
- **Dependency Injection:** Đăng ký qua `services.AddVietQrDeeplink()` trong ASP.NET Core hoặc Generic Host.
- **Tự động nhận diện Endpoint:** Tự động cập nhật domain máy chủ Deeplink qua Discovery Endpoint (có in-memory cache).
- **Hỗ trợ đa nền tảng .NET:** Chạy tốt trên .NET 6.0, .NET 8.0, .NET 9.0, .NET 10.0.

---

## 📦 Cài đặt

```bash
dotnet add package VietQR.Core
dotnet add package VietQR.Deeplink.Client
```

---

## 🚀 Hướng dẫn sử dụng

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

### 2. Tạo link mở App Ngân hàng trong Controller / Service

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
    public async Task<IActionResult> CreatePayment(CancellationToken ct)
    {
        // 1. Tạo chuỗi QR thanh toán chuẩn Napas247 bằng VietQR.Core
        var qr = new VietQr()
            .SetBeneficiaryOrganization("970422", "113366668888", VietQrServiceType.Account)
            .SetTransactionAmount("50000")
            .SetAdditionalData(purpose: "Thanh toan don hang #1024");

        // 2. Lấy link ngắn hạn mở app ngân hàng
        // Tham số thứ 2 chấp nhận: Mã App ("MB"), Tên ngắn ("MBBank") hoặc Mã BIN 6 số ("970422")
        var intent = await qr.ToDeeplinkUrlAsync(_deeplinkClient, "MB", ct);

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

---

## 🔍 Làm sao để biết Ngân hàng / Ví điện tử nào được hỗ trợ?

Bạn có **2 cách** để lấy hoặc kiểm tra danh sách các ứng dụng được hỗ trợ mở Deeplink:

### Cách 1: Lấy bằng code C# qua SDK (`GetSupportedAppsAsync`)

Dùng hàm `_deeplinkClient.GetSupportedAppsAsync()` để lấy danh sách động từ máy chủ (rất tiện để trả về cho Frontend/Mobile dựng màn hình chọn App thanh toán):

```csharp
[HttpGet("supported-apps")]
public async Task<IActionResult> GetSupportedApps(CancellationToken ct)
{
    IReadOnlyList<DeeplinkBankAppInfo> apps = await _deeplinkClient.GetSupportedAppsAsync(ct);

    // Ví dụ lọc ra các ngân hàng hỗ trợ tự động điền thông tin chuyển khoản (Autofill)
    var autofillApps = apps.Where(a => a.SupportsAutofill).ToList();

    return Ok(apps);
}
```

Mỗi phần tử [`DeeplinkBankAppInfo`](https://github.com/Ngochiendev/vietqr-core/blob/main/src/VietQR.Deeplink.Client/Models/DeeplinkBankAppInfo.cs) trả về gồm:

- `AppCode`: Mã truyền vào hàm tạo link (VD: `"MB"`, `"VCB"`, `"TPB"`, `"MOMO"`).
- `Bin`: Mã BIN 6 số chuẩn NAPAS (VD: `"970422"`).
- `ShortName`: Tên viết tắt ngân hàng (VD: `"MBBank"`, `"Vietcombank"`).
- `AppName`: Tên ứng dụng hiển thị (VD: `"MB Bank"`, `"VCB Digibank"`).
- `Logo`: URL ảnh icon ứng dụng ngân hàng sắc nét.
- `SupportsAutofill`: `true` nếu mở thẳng màn hình chuyển khoản và tự điền STK/Số tiền/Nội dung; `false` nếu chỉ mở ứng dụng.

**Kết quả JSON mẫu khi trả về cho Frontend:**

```json
{
    "code": "00",
    "desc": "success",
    "data": [
        {
            "appCode": "MB",
            "bin": "970422",
            "shortName": "MBBank",
            "appName": "MB Bank",
            "logo": "https://deeplink.example.com/icons/mb.png",
            "supportsAutofill": true
        },
        {
            "appCode": "VCB",
            "bin": "970436",
            "shortName": "Vietcombank",
            "appName": "VCB Digibank",
            "logo": "https://deeplink.example.com/icons/vcb.png",
            "supportsAutofill": false
        }
    ]
}
```

### Cách 2: Gọi trực tiếp qua HTTP REST API

```http
GET https://deeplink.yourdomain.com/v1/apps
```

_(Hoặc qua dịch vụ `VietQR.Api`: `GET /api/v1/vietqr-core/deeplink/apps`)_

Hệ thống sẽ trả về danh mục ngân hàng kèm logo, mã BIN và cờ `supportsAutofill` (`true` nếu hỗ trợ tự điền).

> 💡 **Mẹo truyền mã:** Khi gọi `ToDeeplinkUrlAsync(_deeplinkClient, appCode)` hoặc `CreateIntentAsync(appCode, qrContent)`, bạn có thể truyền linh hoạt bất kỳ định danh nào: mã `appCode` (VD: `"MB"`), tên viết tắt `shortName` (VD: `"MBBank"`), mã `BIN` 6 số chuẩn NAPAS (VD: `"970422"`), hoặc bí danh `aliases` mà không phân biệt chữ hoa/thường.

---

## 📄 Giấy phép

Phát hành theo giấy phép [MIT License](LICENSE).
