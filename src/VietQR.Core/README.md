# VietQR.Core

[![NuGet](https://img.shields.io/nuget/v/VietQR.Core?style=flat-square&color=004880)](https://www.nuget.org/packages/VietQR.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VietQR.Core?style=flat-square&color=0078D4)](https://www.nuget.org/packages/VietQR.Core)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://github.com/Ngochiendev/vietqr-core)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE)

Thư viện .NET mã nguồn mở **VietQR.Core** tạo và hiển thị mã QR thanh toán cho Việt Nam theo tiêu chuẩn NAPAS247 / EMVCo.

---

## ⚡ Cài đặt (Installation)

```bash
dotnet add package VietQR.Core
```

---

## 🚀 Bắt đầu nhanh (Quick Start - 30 giây)

### 1. Tạo chuỗi mã QR thanh toán (EMVCo)

Chỉ 1 dòng lệnh, không cần cấu hình phức tạp:

```csharp
using VietQR.Core;

// Tạo mã thanh toán động (chuyển khoản tới số tài khoản MBBank)
string qrContent = VietQrPayload.Create(
    bin: "970422",                  // Mã BIN ngân hàng (MBBank: 970422)
    accountNo: "056789799999",       // Số tài khoản người nhận
    amount: 50000,                   // Số tiền VNĐ (> 0 cho QR động, null cho QR tĩnh)
    description: "Ung ho quy"        // Nội dung chuyển khoản (tự bỏ dấu tiếng Việt)
);

Console.WriteLine(qrContent);
// Output: 00020101021238570010A000000727012700069704220112056789799999...63048E1D
```

---

### 2. Xuất ảnh thẻ thanh toán hiển thị lên UI hoặc lưu file

```csharp
using VietQR.Core;

var imageService = new QrImageService();

// Xuất Data URL Base64 (gắn trực tiếp vào thẻ <img src="..."> trên Web hoặc PictureBox WinForms)
string dataUrl = await imageService.GenerateVietQrBase64Async(
    emvQrContent: qrContent,
    bankBin: "970422",
    accountNo: "056789799999",
    amount: "50000",
    description: "Ung ho quy",
    template: "compact"             // compact (mặc định) | print | qr_only
);

// Hoặc lưu thẳng ra file ảnh PNG / WebP trên ổ cứng
var options = new QrRenderOptions
{
    Template = "compact",
    BankBin = "970422",
    AccountNo = "056789799999",
    Amount = 50000,
    Description = "Ung ho quy"
};
byte[] pngBytes = await imageService.RenderBytesAsync(qrContent, options);
await File.WriteAllBytesAsync("vietqr.png", pngBytes);
```

---

## 🖼️ Mẫu Thẻ Thanh Toán Hỗ Trợ (Visual Showcase)

|                                                Mẫu 1: `compact` (Mặc định)                                                 |                                        Mẫu 2: `print` (Quầy thu ngân / A4 / A5)                                        |                                       Mẫu 3: `card` (Chuyển tiền qua Thẻ ATM)                                        |
| :------------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------: | :------------------------------------------------------------------------------------------------------------------: |
| ![compact](https://cdn.jsdelivr.net/gh/Ngochiendev/vietqr-core@main/src/VietQR.Core/Resources/examples/vietqr-compact.png) | ![print](https://cdn.jsdelivr.net/gh/Ngochiendev/vietqr-core@main/src/VietQR.Core/Resources/examples/vietqr-print.png) | ![card](https://cdn.jsdelivr.net/gh/Ngochiendev/vietqr-core@main/src/VietQR.Core/Resources/examples/vietqr-card.png) |
|                                  Header VietQR, logo Napas247 & logo ngân hàng nhúng sẵn                                   |                                  In to rõ số tiền, tên chủ TK, nội dung chuyển khoản                                   |                                      Chuyển nhanh qua số thẻ Napas (`QRIBFTTC`)                                      |

|                                            Mẫu 4: `custom_logo` (Logo thương hiệu tâm)                                             |                                          Mẫu 5: `qr_only` (Mã QR vuông tối giản)                                          |
| :--------------------------------------------------------------------------------------------------------------------------------: | :-----------------------------------------------------------------------------------------------------------------------: |
| ![custom-logo](https://cdn.jsdelivr.net/gh/Ngochiendev/vietqr-core@main/src/VietQR.Core/Resources/examples/vietqr-custom-logo.png) | ![qr-only](https://cdn.jsdelivr.net/gh/Ngochiendev/vietqr-core@main/src/VietQR.Core/Resources/examples/vietqr-qronly.png) |
|                                      Chèn logo doanh nghiệp ở tâm, giữ mã QR chuẩn đen trắng                                       |                                   Vuông tối giản (`Size = 500`), có icon VietQR ở giữa                                    |

---

## 📋 Bảng Tra Cứu Tham Số Chi Tiết (Parameter Reference)

### 1. Tham số tạo mã thanh toán: `VietQrPayload.Create()`

| Tên tham số   | Kiểu dữ liệu         |   Bắt buộc   | Mô tả & Ví dụ                                                                                        |
| :------------ | :------------------- | :----------: | :--------------------------------------------------------------------------------------------------- |
| `bin`         | `BankBin` / `string` | **Bắt buộc** | Mã BIN ngân hàng đúng 6 chữ số (VD: `"970422"` - MBBank, `"970436"` - VCB, `"970415"` - VietinBank). |
| `accountNo`   | `string`             | **Bắt buộc** | Số tài khoản, số thẻ hoặc mã tài khoản định danh/ảo (Tối đa 25 ký tự).                               |
| `amount`      | `long?`              |   Tùy chọn   | Số tiền thanh toán VNĐ ($> 0$). Bỏ trống (`null`) để tạo **QR tĩnh** (người quét tự gõ số tiền).     |
| `description` | `string?`            |   Tùy chọn   | Nội dung thanh toán (Tối đa 95 ký tự EMVCo Tag 62.08, tự động loại bỏ dấu tiếng Việt).               |
| `serviceType` | `VietQrServiceType`  |   Tùy chọn   | Loại hình chuyển khoản: `Account` (mặc định - QRIBFTTA) hoặc `Card` (QRIBFTTC chuyển qua thẻ).       |

---

### 2. Tùy chọn render ảnh thẻ: `QrRenderOptions` class

| Tên thuộc tính     | Kiểu dữ liệu    | Mặc định    | Mô tả chi tiết                                                                                |
| :----------------- | :-------------- | :---------- | :-------------------------------------------------------------------------------------------- |
| `Template`         | `string`        | `"compact"` | Mẫu thiết kế ảnh thẻ: `"compact"` (thẻ chuẩn), `"print"` (mẫu in quầy), `"qr_only"` (chỉ QR). |
| `Format`           | `QrImageFormat` | `Png`       | Định dạng file ảnh xuất ra: `QrImageFormat.Png`, `Webp` hoặc `Svg`.                           |
| `Size`             | `int`           | `500`       | Kích thước cạnh vuông tính bằng pixel (chỉ áp dụng cho template `"qr_only"`).                 |
| `Quality`          | `int`           | `100`       | Chất lượng nén ảnh (từ 1 đến 100, đặc biệt hữu ích khi xuất file WebP).                       |
| `AccountName`      | `string?`       | `null`      | Tên chủ tài khoản người nhận (hiển thị nổi bật trên mẫu in ấn `"print"`).                     |
| `CustomCenterLogo` | `byte[]?`       | `null`      | Mảng byte ảnh logo riêng đặt ở tâm QR (thay thế cho logo VietQR mặc định).                    |
| `CenterLogoRatio`  | `float`         | `0.18f`     | Tỷ lệ kích thước logo ở tâm so với mã QR (từ 0.05 đến 0.35, đảm bảo mắt đọc quét tốt).        |
| `BillNumber`       | `string?`       | `null`      | Số hóa đơn / mã đơn hàng (trích xuất đưa vào Tag 62.01).                                      |

---

### 3. Dữ liệu nhận được khi giải mã: `VietQrInfo` class

_(Kết quả trả về từ `VietQrDecoder.Decode` quét ảnh hoặc `VietQrParser.Parse` bóc tách chuỗi)_

| Tên thuộc tính | Kiểu dữ liệu        | Ý nghĩa nghiệp vụ                                                                          |
| :------------- | :------------------ | :----------------------------------------------------------------------------------------- |
| `BankBin`      | `string`            | Mã BIN 6 số của ngân hàng thụ hưởng (VD: `"970422"`).                                      |
| `BankInfo`     | `BankInfo?`         | Thông tin ngân hàng: tên đầy đủ (`Name`), tên viết tắt (`ShortName`), logo nhúng (`Logo`). |
| `AccountNo`    | `string`            | Số tài khoản hoặc số thẻ của người nhận tiền.                                              |
| `Amount`       | `decimal?`          | Số tiền giao dịch thanh toán (null nếu là mã QR tĩnh).                                     |
| `Description`  | `string?`           | Nội dung chuyển khoản trích xuất từ Tag 62.08.                                             |
| `BillNumber`   | `string?`           | Số hóa đơn / mã đơn hàng trích xuất từ Tag 62.01.                                          |
| `ServiceType`  | `VietQrServiceType` | `Account` (chuyển tới STK) hoặc `Card` (chuyển tới số thẻ Napas).                          |
| `QrType`       | `VietQrType`        | `Static` (QR tĩnh quét tự nhập tiền) hoặc `Dynamic` (QR động có sẵn tiền).                 |
| `IsValidCrc`   | `bool`              | `true` nếu mã băm kiểm tra CRC16 (Tag 63) hợp lệ 100% theo chuẩn EMVCo.                    |

---

## 💡 Các Kịch Bản Sử Dụng Thường Gặp (Common Use Cases)

### 1. Tạo QR tĩnh vs QR động

```csharp
// QR Tĩnh (Static): Dán tại quầy thu ngân / xe bán hàng, khách tự nhập số tiền
string staticQr = VietQrPayload.Create("970422", "056789799999", description: "Cua hang A");

// QR Động (Dynamic): Gắn sẵn số tiền cho từng đơn hàng cụ thể
string dynamicQr = VietQrPayload.Create("970422", "056789799999", amount: 150000, description: "DH1024");
```

### 2. Chuyển tiền tới Số Thẻ ATM Napas (thay vì STK)

```csharp
string cardQr = VietQrPayload.Create(
    bin: "970422",
    accountNo: "9704222028493997",   // Số in dập nổi trên thẻ vật lý
    amount: 100000,
    serviceType: VietQrServiceType.Card // Chuyển sang chuẩn QRIBFTTC
);
```

### 3. Tra cứu danh mục 76+ ngân hàng (Chạy Offline 100%)

```csharp
var catalog = EmbeddedBankCatalog.Instance;

// Tra cứu bằng mã BIN, mã Code hoặc tên viết tắt (không phân biệt hoa thường)
BankInfo? bank = await catalog.FindAsync("970422");
// bank.ShortName == "MBBank", bank.Code == "MB", bank.Name == "Ngân hàng TMCP Quân Đội"

// Lấy danh sách toàn bộ 76+ ngân hàng
IReadOnlyList<BankInfo> allBanks = await catalog.GetAllAsync();
```

### 4. Quét và giải mã ảnh VietQR (`VietQrDecoder`)

Hỗ trợ đọc từ ảnh chụp màn hình, ảnh upload từ khách hàng (hỗ trợ cả ảnh VietQR ngân hàng và ShopeePay):

```csharp
byte[] imageBytes = await File.ReadAllBytesAsync("qr_screenshot.png");

Result<VietQrInfo> result = VietQrDecoder.Decode(imageBytes);
if (result.IsSuccess)
{
    VietQrInfo info = result.Value!;
    Console.WriteLine($"Ngân hàng: {info.BankInfo?.ShortName} - STK: {info.AccountNo}");
    Console.WriteLine($"Số tiền: {info.Amount:N0} VNĐ - Nội dung: {info.Description}");
}
```

### 5. Đăng ký Dependency Injection trong ASP.NET Core

```csharp
// Trong Program.cs
builder.Services.AddVietQrCore();

// Inject vào Controller hoặc Minimal API
app.MapGet("/api/qr", async (IQrGenerator generator, IQrImageRenderer renderer) =>
{
    var request = new VietQrRequest(BankBin.Create("970422"), "056789799999", 50000, "Thanh toan");
    string payload = generator.Generate(request);
    byte[] image = await renderer.RenderPngAsync(payload, new QrRenderOptions { Template = "compact" });
    return Results.File(image, "image/png");
});
```

---

## 🌟 Ưu Điểm của VietQR.Core

- 🚀 **Hiệu năng cao:** Tính toán chuỗi EMVCo và bảng băm CRC16 chỉ mất ~0.4 micro-giây ($\mu$s), tối ưu hóa `ReadOnlySpan<char>` không cấp phát rác bộ nhớ.
- 📦 **Hỗ trợ toàn diện .NET:** Tương thích `.NET 6.0`, `.NET 8.0` (LTS), `.NET 9.0` và `.NET 10.0`.

---

## 🙏 Lời Cảm Ơn (Acknowledgments)

- **[NAPAS](https://napas.com.vn)** & **[Sáng kiến VietQR](https://vietqr.net)** vì quy chuẩn kỹ thuật thanh toán số thông minh.
- **[VietQR.io](https://vietqr.io)** vì dữ liệu danh mục ngân hàng tham khảo và hệ thống CDN logo ngân hàng tiện ích cho cộng đồng.

---

## 📄 Bản Quyền & Giấy Phép

- Phát hành theo giấy phép mã nguồn mở **[MIT License](https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE)** — Hoàn toàn miễn phí cho cả dự án cá nhân và thương mại.
- _VietQR® và NAPAS247® là nhãn hiệu thuộc sở hữu của NAPAS. Dự án này là thư viện mã nguồn mở độc lập._
