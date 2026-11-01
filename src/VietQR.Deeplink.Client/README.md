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

### Cách 2: Tra cứu nhanh bảng 43 Ứng dụng dưới đây

Khi gọi `ToDeeplinkUrlAsync(_deeplinkClient, appCode)` hoặc `CreateIntentAsync(appCode, qrContent)`, bạn có thể truyền **bất kỳ giá trị nào** trong cột **Mã `appCode`**, **Tên ngắn (`shortName`)**, **Mã BIN**, hoặc **Bí danh (`aliases`)** (không phân biệt chữ hoa/thường).

> 💡 **Ghi chú chế độ mở App:**
>
> - **✅ Autofill:** Mở thẳng màn hình chuyển khoản trong App và tự động điền sẵn Ngân hàng nhận, Số tài khoản, Số tiền, Nội dung chuyển khoản.
> - **📱 Mở App:** Mở trực tiếp ứng dụng ngân hàng/ví điện tử trên điện thoại (iOS/Android), tự động điều hướng App Store / Google Play nếu máy chưa cài đặt. Trên máy tính (Desktop), đường dẫn tự động hiển thị trang quét mã QR thanh toán.

#### 🏦 1. Ngân hàng thương mại (33 ngân hàng)

|  Mã `appCode`  | Tên ngắn (`shortName`) | Tên ứng dụng (`appName`)  |  Mã BIN  | Bí danh khác (`aliases`)    |   Chế độ    |
| :------------: | :--------------------- | :------------------------ | :------: | :-------------------------- | :---------: |
|    **`MB`**    | `MBBank`               | MB Bank                   | `970422` | `MBBANK`                    | ✅ Autofill |
|   **`TPB`**    | `TPBank`               | TPBank Mobile             | `970423` | `TPBANK`                    | ✅ Autofill |
|   **`VCB`**    | `Vietcombank`          | VCB Digibank              | `970436` | `VIETCOMBANK`               |  📱 Mở App  |
|   **`BIDV`**   | `BIDV`                 | BIDV SmartBanking         | `970418` | —                           |  📱 Mở App  |
|   **`ICB`**    | `VietinBank`           | VietinBank iPay           | `970415` | `VIETINBANK`, `CTG`         |  📱 Mở App  |
|   **`VBA`**    | `Agribank`             | Agribank Plus             | `970405` | `AGRIBANK`                  |  📱 Mở App  |
|   **`TCB`**    | `Techcombank`          | Techcombank Mobile        | `970407` | `TECHCOMBANK`               |  📱 Mở App  |
|   **`ACB`**    | `ACB`                  | ACB ONE                   | `970416` | —                           |  📱 Mở App  |
|   **`VPB`**    | `VPBank`               | VPBank NEO                | `970432` | `VPBANK`                    |  📱 Mở App  |
|   **`VIB`**    | `VIB`                  | MyVIB                     | `970441` | —                           |  📱 Mở App  |
|   **`STB`**    | `Sacombank`            | SACOMBANK PAY             | `970403` | `SACOMBANK`                 |  📱 Mở App  |
|   **`HDB`**    | `HDBank`               | HDBank                    | `970437` | `HDBANK`                    |  📱 Mở App  |
|   **`SHB`**    | `SHB`                  | SHB SAHA                  | `970443` | —                           |  📱 Mở App  |
|   **`LPB`**    | `LPBank`               | LPBank Plus               | `970449` | `LPBANK`                    |  📱 Mở App  |
|   **`OCB`**    | `OCB`                  | OCB OMNI                  | `970448` | —                           |  📱 Mở App  |
|   **`MSB`**    | `MSB`                  | MSB mBank                 | `970426` | —                           |  📱 Mở App  |
|   **`SEAB`**   | `SeABank`              | SeAMobile                 | `970440` | `SEABANK`                   |  📱 Mở App  |
|   **`EIB`**    | `Eximbank`             | Eximbank EDigi            | `970431` | `EXIMBANK`                  |  📱 Mở App  |
|   **`ABB`**    | `ABBANK`               | ABBank                    | `970425` | `ABBANK`                    |  📱 Mở App  |
|   **`PVCB`**   | `PVcomBank`            | PVConnect                 | `970412` | `PVCOMBANK`                 |  📱 Mở App  |
|   **`NAB`**    | `NamABank`             | Nam A Bank Open Banking   | `970428` | `NAMABANK`                  |  📱 Mở App  |
|   **`VCCB`**   | `BVBank`               | digimi - Ngân hàng số     | `970454` | `BVBANK`, `VIETCAPITALBANK` |  📱 Mở App  |
| **`VIETBANK`** | `VietBank`             | Vietbank Digital Plus     | `970433` | —                           |  📱 Mở App  |
|   **`BAB`**    | `BacABank`             | BAC A BANK Mobile Banking | `970409` | `BACABANK`                  |  📱 Mở App  |
|   **`NCB`**    | `NCB`                  | NCB iziMobile             | `970419` | —                           |  📱 Mở App  |
|   **`SCB`**    | `SCB`                  | SCB Mobile Banking        | `970429` | —                           |  📱 Mở App  |
|   **`KLB`**    | `KienLongBank`         | KienlongBank Plus         | `970452` | `KIENLONGBANK`              |  📱 Mở App  |
|   **`BVB`**    | `BaoVietBank`          | BAOVIET Smart             | `970438` | `BAOVIETBANK`               |  📱 Mở App  |
|  **`SGICB`**   | `SaigonBank`           | SAIGONBANK Smart Banking  | `970400` | `SAIGONBANK`                |  📱 Mở App  |
|  **`SHBVN`**   | `ShinhanBank`          | Shinhan SOL Việt Nam      | `970424` | `SHINHANBANK`, `SHINHAN`    |  📱 Mở App  |
|   **`WVN`**    | `Woori`                | Woori WON Vietnam         | `970457` | `WOORI`, `WOORIBANK`        |  📱 Mở App  |
|   **`PBVN`**   | `PublicBank`           | PB engage VN              | `970439` | `PUBLICBANK`                |  📱 Mở App  |
|   **`CIMB`**   | `CIMB`                 | Octo by CIMB Vietnam      | `422589` | —                           |  📱 Mở App  |
| **`COOPBANK`** | `COOPBANK`             | Co-opBank Mobile Banking  | `970446` | —                           |  📱 Mở App  |

#### 📱 2. Ngân hàng số (4 ngân hàng số)

| Mã `appCode`  | Tên ngắn (`shortName`) | Tên ứng dụng (`appName`)      |  Mã BIN  | Bí danh khác (`aliases`) |  Chế độ   |
| :-----------: | :--------------------- | :---------------------------- | :------: | :----------------------- | :-------: |
|  **`CAKE`**   | `CAKE`                 | CAKE - Ngân hàng số           | `546034` | —                        | 📱 Mở App |
|  **`TIMO`**   | `Timo`                 | Timo - Ngân hàng số by BVBank | `963388` | —                        | 📱 Mở App |
|  **`TNEX`**   | `TNEX`                 | TNEX - Tài chính số toàn diện | `963326` | —                        | 📱 Mở App |
| **`LIOBANK`** | `Liobank`              | Liobank: Ngân hàng số         | `970448` | `LIO`                    | 📱 Mở App |

#### 👛 3. Ví điện tử & Trung gian thanh toán (6 ứng dụng)

|  Mã `appCode`   | Tên ngắn (`shortName`) | Tên ứng dụng (`appName`)         |  Mã BIN  | Bí danh khác (`aliases`)     |  Chế độ   |
| :-------------: | :--------------------- | :------------------------------- | :------: | :--------------------------- | :-------: |
|   **`MOMO`**    | `MoMo`                 | MoMo - Trợ Thủ Tài Chính         | `971025` | —                            | 📱 Mở App |
|  **`ZALOPAY`**  | `ZaloPay`              | Zalopay - Thanh toán & Tài chính | `971101` | —                            | 📱 Mở App |
|   **`VNPAY`**   | `VNPAY`                | Ứng dụng VNPAY                   |    —     | `VIVNPAY`                    | 📱 Mở App |
| **`SHOPEEPAY`** | `ShopeePay`            | ShopeePay - Tài Chính Dễ Dàng    |    —     | `AIRPAY`                     | 📱 Mở App |
| **`VTLMONEY`**  | `ViettelMoney`         | Viettel Money                    | `971005` | `VIETTELMONEY`, `VIETTELPAY` | 📱 Mở App |
| **`VNPTMONEY`** | `VNPTMoney`            | VNPT Money                       | `971011` | `VNPTPAY`                    | 📱 Mở App |

---

## 📄 Giấy phép

Phát hành theo giấy phép [MIT License](LICENSE).
