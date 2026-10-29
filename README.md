# VietQR.Core — Hệ Sinh Thái Mã QR Thanh Toán NAPAS247 cho .NET

<p align="center">
  <img src="src/VietQR.Core/Resources/vietqr.png" alt="VietQR Logo" width="160" />
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="src/VietQR.Core/Resources/napas247.png" alt="NAPAS247 Logo" width="160" />
</p>

<p align="center">
  <b>Giải pháp mã nguồn mở cho thanh toán VietQR (NAPAS247 &amp; EMVCo) trên nền tảng .NET</b><br>
  Danh mục 76+ ngân hàng &amp; font Unicode nhúng sẵn trong DLL &bull; Render PNG / WebP / SVG &bull; Giải mã từ chuỗi hoặc ảnh
</p>

<p align="center">
  <a href="https://github.com/Ngochiendev/vietqr-core/actions/workflows/ci.yml"><img src="https://github.com/Ngochiendev/vietqr-core/actions/workflows/ci.yml/badge.svg" alt="CI Status" /></a>
  <a href="https://www.nuget.org/packages/VietQR.Core"><img src="https://img.shields.io/nuget/v/VietQR.Core?style=flat-square&color=004880" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/VietQR.Core"><img src="https://img.shields.io/nuget/dt/VietQR.Core?style=flat-square&color=0078D4" alt="NuGet Downloads" /></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square" alt=".NET Support" /></a>
  <a href="https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License MIT" /></a>
</p>

---

## 🖼️ Mẫu Thẻ Thanh Toán Thực Tế (Visual Showcase)

Toàn bộ ảnh mẫu được sinh tự động bằng chính thư viện `VietQR.Core`:

| Mẫu 1: `compact` (Mặc định) | Mẫu 2: `print` (Quầy thu ngân) | Mẫu 3: `card` (Thẻ ATM NAPAS) |
| :---: | :---: | :---: |
| <img src="docs/image/compact.png" alt="Mẫu Compact Tài khoản" width="220" /> | <img src="docs/image/print.png" alt="Mẫu In quầy thu ngân" width="220" /> | <img src="docs/image/card.png" alt="Mẫu Chuyển tiền qua Thẻ" width="220" /> |
| **Chuyển nhanh đến STK**<br>Header VietQR, logo Napas & Ngân hàng. | **Thẻ in POS / Để bàn**<br>In to rõ Số tiền (VNĐ), Nội dung CK, Tên chủ TK. | **Chuyển nhanh đến Thẻ**<br>Định danh `QRIBFTTC`, tự động chuyển nhãn `"Số thẻ"`. |

| Mẫu 4: Tùy biến Logo Đối tác | Mẫu 5: `qr_only` (Mã QR vuông) |
| :---: | :---: |
| <img src="docs/image/custom_logo.png" alt="Custom Center Logo" width="220" /> | <img src="docs/image/qr_only.png" alt="Mã QR vuông tối giản" width="220" /> |
| **Nhúng Logo Riêng ở Tâm**<br>Giữ mã QR đen trắng chuẩn (`#000000`/`#FFFFFF`). | **Mã QR Vuông Tinh Gọn**<br>Kích thước tùy chỉnh (`Size = 500`), có icon VietQR ở giữa. |

---

## 🚀 Khởi Chạy Web API Trực Quan (Quick Start with API)

Dự án có sẵn một Web API hoàn chỉnh. Clone repo và chạy trong 10 giây:

```bash
# 1. Clone mã nguồn
git clone https://github.com/Ngochiendev/vietqr-core.git
cd vietqr-core

# 2. Khởi chạy Web API
dotnet run --project src/VietQR.Api
```

Mở trình duyệt truy cập Swagger UI:
👉 **`http://localhost:5000/swagger`** (hoặc port hiển thị trên console).

### Các Endpoint chính:
- **Tạo mã QR & Render ảnh:**
  `GET /api/v1/qr/generate?bin=970422&accountNo=056789799999&amount=2000&description=Thanh+toan+cafe`
- **Tra cứu danh sách ngân hàng thành viên:**
  `GET /api/v1/banks`

---

## 📦 Sử Dụng Package NuGet Trong Ứng Dụng Khác

```bash
dotnet add package VietQR.Core
```

👉 **Hướng dẫn chi tiết dành cho package (ảnh + code mẫu đầy đủ):** [src/VietQR.Core/README.md](src/VietQR.Core/README.md) — nội dung này cũng hiển thị trên trang [NuGet.org/packages/VietQR.Core](https://www.nuget.org/packages/VietQR.Core).

---

## 💡 Hướng Dẫn Code Mẫu Cốt Lõi

### 1. Tạo chuỗi VietQR & Render ảnh thẻ

```csharp
using VietQR.Core;

// 1. Tạo chuỗi mã QR Động NAPAS247 (Tài khoản MBBank, gắn sẵn số tiền)
string payload = VietQrPayload.CreateDynamic(
    BankBin.Create("970422"),                // MBBank
    "056789799999",                          // Số tài khoản
    amount: 2000,
    description: "Thanh toan cafe");

// 2. Render ảnh thẻ thanh toán (hoàn toàn offline — logo & font nằm trong DLL)
var imageService = new QrImageService();
byte[] pngBytes = await imageService.RenderBytesAsync(payload, new QrRenderOptions
{
    Template = "compact",
    BankBin = "970422",
    AccountNo = "056789799999",
    AccountName = "VO NGOC HIEN",
    Amount = 2000,
    Description = "Thanh toan cafe"
});

await File.WriteAllBytesAsync("vietqr.png", pngBytes);
```

Chuỗi EMVCo sinh ra theo mẫu chuẩn NAPAS247 (có sẵn CRC Tag 63 ở cuối):

```text
00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA
5303704540420005802VN62180814thanh toan test6304xxxx
```

### 2. Chuyển tiền qua Thẻ (Card - `QRIBFTTC`)

```csharp
// Luôn truyền TƯỜNG MINH serviceType: VietQrServiceType.Card
string cardPayload = VietQrPayload.CreateDynamic(
    BankBin.Create("970422"),
    "9704222028493997",                      // Số thẻ NAPAS
    amount: 1000,
    serviceType: VietQrServiceType.Card,     // Định danh mã dịch vụ QRIBFTTC
    description: "Chuyen vao the MB");

// Render ảnh — tự động đổi nhãn "Số TK" thành "Số thẻ"
byte[] cardBytes = await imageService.RenderBytesAsync(cardPayload, new QrRenderOptions
{
    Template = "compact",
    BankBin = "970422",
    AccountNo = "9704222028493997",
    AccountName = "VO NGOC HIEN",
    Amount = 1000,
    ServiceType = VietQrServiceType.Card
});
```

### 3. Giải mã ngược mã QR từ chuỗi hoặc ảnh (Parser / Decoder)

```csharp
// (a) Phân tích từ chuỗi EMVCo — kiểm tra checksum CRC16:
Result<VietQrInfo> result = VietQrParser.Parse(payload);
if (result.IsSuccess)
{
    VietQrInfo info = result.Value!;
    Console.WriteLine($"Ngân hàng: {info.BankInfo?.ShortName}");
    Console.WriteLine($"Số TK/Thẻ: {info.AccountNo}");
    Console.WriteLine($"Số tiền: {info.Amount:N0} {info.Currency}");
    Console.WriteLine($"CRC16 hợp lệ: {info.IsValidCrc}");
}

// (b) Đọc từ ảnh tải lên (PNG, WebP, JPEG, BMP):
byte[] imageBytes = await File.ReadAllBytesAsync("customer_uploaded_qr.png");
Result<VietQrInfo> decoded = VietQrDecoder.Decode(imageBytes);
```

### 4. Xuất WebP / SVG / Stream (tối ưu server)

```csharp
// WebP — dung lượng nhỏ hơn PNG khoảng 30-40%
byte[] webp = await imageService.RenderWebpBytesAsync(payload,
    new QrRenderOptions { Template = "compact", Quality = 80 });
await imageService.SaveWebpAsync("vietqr.webp", payload);

// SVG vector — thuần C# (không cần Skia native), dùng cho in ấn decal khổ lớn
string svgXml = VietQrSvgRenderer.Render(payload, size: 500);

// Stream — ghi trực tiếp ra Response, không cấp phát byte[] lên LOH
await imageService.RenderToStreamAsync(payload, httpContext.Response.Body,
    new QrRenderOptions { Format = QrImageFormat.Webp });
```

---

## 🔌 Tích Hợp ASP.NET Core (Dependency Injection)

```csharp
// Program.cs
using VietQR.Core;

builder.Services.AddVietQrCore(options =>
{
    options.DefaultTemplate = "compact";
});
```

```csharp
// Minimal API hoặc Controller — inject trực tiếp
app.MapGet("/api/qr", async (QrImageService qrService, string bin, string stk, long amount) =>
{
    string payload = VietQrPayload.Create(BankBin.Create(bin), stk, amount);
    byte[] image = await qrService.RenderBytesAsync(payload, new QrRenderOptions
    {
        Template = "compact",
        BankBin = bin,
        AccountNo = stk,
        Amount = amount
    });
    return Results.File(image, "image/png");
});
```

---

## 🤝 Đóng Góp & Quy Chuẩn Commit (Contributing)

Dự án rất hoan nghênh mọi đóng góp từ cộng đồng (Issue, Pull Request, Feature Request).
Chi tiết xem tại [CONTRIBUTING.md](CONTRIBUTING.md).

### Quy ước Commit (Conventional Commits):
```text
<type>(<scope>): <mô tả ngắn gọn>
```

- `feat`: Thêm chức năng mới (VD: `feat(rendering): add webp support`)
- `fix`: Sửa lỗi (VD: `fix(parser): fix padding issue in tag 62`)
- `perf`: Tối ưu hiệu năng (VD: `perf(crc): optimize lookup table`)
- `docs`: Cập nhật tài liệu (VD: `docs: update visual showcase in readme`)
- `test`: Thêm hoặc sửa test case

---

## 📄 Giấy Phép (License)

Dự án được phân phối dưới giấy phép mã nguồn mở **[MIT License](LICENSE)**.
Bản quyền thuộc về © 2026 **Ngochiendev**.
