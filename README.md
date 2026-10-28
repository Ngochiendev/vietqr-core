# VietQR Core (NAPAS247)

<p align="center">
  <img src="src/VietQR.Core/Resources/vietqr.png" alt="VietQR Logo" width="160" />
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="src/VietQR.Core/Resources/napas247.png" alt="NAPAS247 Logo" width="160" />
</p>

<p align="center">
  <b>Thư viện .NET và Web API hỗ trợ tạo chuỗi & render ảnh mã QR thanh toán theo chuẩn VietQR (NAPAS247)</b><br>
  .NET 10 / 8 / 6 &bull; SkiaSharp &bull; Chạy Offline 100% &bull; Tối ưu hiệu năng
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/VietQR.Core"><img src="https://img.shields.io/nuget/v/VietQR.Core.svg?style=flat-square&logo=nuget&color=004880" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/VietQR.Core"><img src="https://img.shields.io/nuget/dt/VietQR.Core.svg?style=flat-square&logo=nuget&color=0078D4" alt="NuGet Downloads" /></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-10.0_%7C_8.0_%7C_6.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET Support" /></a>
  <a href="#"><img src="https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp&logoColor=white" alt="C# 12" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Standard-NAPAS247_EMVCo-005BAA?style=flat-square" alt="Standard NAPAS247" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Graphics-SkiaSharp_4.x-E65100?style=flat-square" alt="SkiaSharp 4.x" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Tests-534%2B_passed-2ea44f?style=flat-square&logo=githubactions&logoColor=white" alt="Tests Passed" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-0366d6?style=flat-square" alt="License MIT" /></a>
</p>

---

## 🖼 Bảng Ảnh Mẫu 3 Template VietQR

Hệ thống hỗ trợ 3 mẫu giao diện xuất ra (Base64 PNG) được lưu tại [`docs/image/`](docs/image):

|                                                        `qr_only` (500 × 500 px)                                                        |                                                 `compact` (600 × 585 px) - _Mặc định_                                                  |                                                       `print` (600 × 745 px)                                                       |
| :------------------------------------------------------------------------------------------------------------------------------------: | :------------------------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------------------------------------------: |
| <img src="docs/image/qr_only.png" alt="Template QR Only" width="220" /> | <img src="docs/image/compact.png" alt="Template Compact" width="220" /> | <img src="docs/image/print.png" alt="Template Print" width="220" /> |
|                                                            **Mẫu Tinh Gọn**                                                            |                                                         **Mẫu Thẻ Tiêu Chuẩn**                                                         |                                                       **Mẫu In Ấn Mở Rộng**                                                        |
|                                               Chỉ gồm mã QR vuông kèm logo VietQR ở tâm.                                               |                              Thẻ thanh toán đầy đủ Header VietQR, khung viền, logo NAPAS247 & Ngân hàng.                               |         Hiển thị chi tiết: Số tiền (VNĐ), Nội dung CK, Tên chủ TK, Số TK in đậm, Tên NH. Tự co giãn font chống tràn viền.          |

---

## ⚡ Tính Năng Chính

- **Chuẩn VietQR (NAPAS247):** Hỗ trợ chuyển khoản nhanh qua số tài khoản, số thẻ và tài khoản định danh / thu hộ (Virtual Account).
- **Hoạt động Offline 100%:** Tạo chuỗi và vẽ ảnh QR hoàn toàn độc lập, không phụ thuộc internet khi xử lý.
- **Tốc độ cao & Nhẹ tài nguyên:** Xử lý tức thì (~0.0001 ms/mã), tối ưu bộ nhớ cho hệ thống chịu tải lớn.
- **Render ảnh thẻ sắc nét:** Đồ họa SkiaSharp chất lượng cao, tự động căn chỉnh và khử viền logo ngân hàng.
- **Nhúng sẵn 76+ Ngân hàng (Embedded Resources):** Toàn bộ 76 logo ngân hàng thành viên NAPAS được đóng gói trực tiếp vào DLL, đạt chuẩn Zero-I/O & chạy offline 100%.

---

## 📦 Thư Viện NuGet: [VietQR.Core](https://www.nuget.org/packages/VietQR.Core)

[![NuGet Version](https://img.shields.io/nuget/v/VietQR.Core.svg?style=for-the-badge&logo=nuget&color=004880)](https://www.nuget.org/packages/VietQR.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VietQR.Core.svg?style=for-the-badge&logo=nuget&color=0078D4)](https://www.nuget.org/packages/VietQR.Core)

Cài đặt package chính thức từ **[NuGet Gallery](https://www.nuget.org/packages/VietQR.Core)**:

```bash
# .NET CLI
dotnet add package VietQR.Core

# Package Manager Console
Install-Package VietQR.Core

# PackageReference trong file .csproj
<PackageReference Include="VietQR.Core" Version="1.0.1" />
```

### 💻 Code Mẫu

Đoạn code hoàn chỉnh tạo chuỗi EMVCo và render ra ảnh thẻ thanh toán VietQR (chạy độc lập trong Console, Worker, WinForms hoặc Web):

```csharp
using VietQR.Core.Emv;
using VietQR.Core.Rendering;

// 1. Tạo chuỗi mã thanh toán chuẩn EMVCo NAPAS247
var qrCode = new VietQr()
    .SetBeneficiaryOrganization(
        acquirerId: "970422",                           // Mã BIN ngân hàng (6 số - VD: 970422 MBBank)
        consumerId: "056789799999",                     // STK hoặc Virtual Account (tối đa 25 ký tự)
        serviceType: VietQrServiceType.Account)         // Chuyển nhanh qua tài khoản
    .SetTransactionAmount("50000")                      // Số tiền (VNĐ)
    .SetAdditionalDataFieldTemplate("THANH TOAN DON HANG") // Nội dung chuyển khoản
    .Build();

Console.WriteLine($"Chuỗi mã VietQR: {qrCode}");

// 2. Render ảnh thẻ thanh toán (Base64 Data URL hoặc file PNG)
var imageService = new QrImageService();

// Cách A: Lấy chuỗi Base64 Data URL (hiển thị trực tiếp trên HTML <img> hoặc Mobile App)
string base64Url = await imageService.GenerateVietQrBase64Async(
    emvQrContent: qrCode,
    bankName: "MBBank",
    bankBin: "970422",
    template: "compact" // "compact" (mặc định), "qr_only" hoặc "print"
);

// Cách B: Xuất ra file ảnh PNG
byte[] pngBytes = await imageService.RenderPngBytesAsync(
    emvQrContent: qrCode,
    bankBin: "970422",
    template: "compact"
);
await File.WriteAllBytesAsync("vietqr.png", pngBytes);
```

> 💡 **Dành cho dự án ASP.NET Core:** Bạn cũng có thể đăng ký `builder.Services.AddVietQrCore()` vào DI container và inject abstraction `IQrGenerator` vào các Service/Controller.

---

## 🚀 Khởi Chạy Web API Host

Dự án cung cấp sẵn microservice Web API hoàn chỉnh chạy trên .NET 8 Kestrel với đầy đủ Swagger UI, Rate Limiter và Health Checks.

### Cách 1: Chạy trực tiếp từ mã nguồn

```bash
# 1. Khôi phục thư viện và build
dotnet restore
dotnet build -c Release

# 2. Khởi chạy Web API (Cổng mặc định: http://localhost:5187)
dotnet run --project src/VietQR.Api/VietQR.Api.csproj

# 3. Mở Swagger UI trên trình duyệt:
# http://localhost:5187/swagger
```

### Cách 2: Chạy với Docker

```bash
# 1. Build Docker image (chạy dưới non-root user bảo mật)
docker build -t vietqr-api -f src/VietQR.Api/Dockerfile .

# 2. Chạy container (ánh xạ cổng 8080)
docker run -d -p 8080:8080 --name vietqr-service vietqr-api

# 3. Kiểm tra trạng thái container:
curl http://localhost:8080/health
```

---

## 📡 API Tham Chiếu

Base URL: `http://localhost:5187/api/v1/vietqr-core` (hoặc `http://localhost:5187/api/v1/qr`)

### 1. Sinh Mã & Render Ảnh Thẻ

`GET /generate`

| Tham số       | Bắt buộc |   Kiểu   | Mô tả & Ví dụ                                                                |
| :------------ | :------: | :------: | :--------------------------------------------------------------------------- |
| `bankBin`     |  **Có**  | `string` | Mã BIN 6 số của ngân hàng (VD: `970422` - MBBank, `970436` - Vietcombank).   |
| `accountNo`   |  **Có**  | `string` | Số tài khoản, số thẻ hoặc Virtual Account người thụ hưởng (tối đa 25 ký tự). |
| `amount`      |  **Có**  | `string` | Số tiền VNĐ (> 0, tối đa 13 chữ số, VD: `50000`).                            |
| `description` |  **Có**  | `string` | Nội dung chuyển khoản (tối đa 95 ký tự theo chuẩn EMVCo).                    |
| `accountName` |  Không   | `string` | Tên chủ tài khoản (hiển thị trên mẫu `print`, VD: `NGUYEN VAN A`).           |
| `template`    |  Không   | `string` | Mẫu giao diện ảnh: `compact` (mặc định), `qr_only` hoặc `print`.             |

**Ví dụ Request:**

```http
GET /api/v1/vietqr-core/generate?bankBin=970422&accountNo=056789799999&amount=50000&description=hoc%20phi&template=compact
```

**Phản hồi (HTTP 200 OK):**

```json
{
    "code": "00",
    "desc": "success",
    "data": {
        "qrCode": "00020101021238540010A0000007270124000697042201120567897999990208QRIBFTTA53037045405500005802VN62110807hoc phi6304E6C2",
        "qrDataURL": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAlgAAAJJAQMAAACu6UvV..."
    }
}
```

### 2. Các Endpoint Quản Trị & Tra Cứu

- `GET /api/v1/vietqr-core/banks`: Lấy danh mục 76 ngân hàng thành viên NAPAS từ Embedded Bank Catalog (Zero-I/O).
- `GET /api/v1/vietqr-core/banks/{binOrCode}`: Tra cứu ngân hàng theo mã BIN hoặc mã viết tắt (vd: `970422` hoặc `MB`).
- `GET /health` hoặc `GET /health/live`: Health check probe cho Docker/Kubernetes/Render/Railway.
- `GET /api/v1/vietqr-core/self-test`: Kiểm thử tự động chất lượng engine SkiaSharp, CRC16 và chuẩn EMVCo.
- `GET /api/v1/vietqr-core/benchmark-school-test`: Benchmark hiệu năng và tải thực tế trên máy chủ.

---

## 📋 Danh Sách Mã Trạng Thái API

| Code | HTTP | Ý Nghĩa                                                                       |
| :--: | :--: | :---------------------------------------------------------------------------- |
| `00` | 200  | **Thành công** (kèm chuỗi `qrCode` và ảnh Base64 `qrDataURL`).                |
| `01` | 400  | **Thiếu tham số bắt buộc** (`bankBin`, `accountNo`, `amount`, `description`). |
| `02` | 400  | **Số tiền không hợp lệ** (phải là số nguyên dương > 0, tối đa 13 số).         |
| `03` | 400  | **Định dạng không hợp lệ** (Mã BIN khác 6 số, hoặc STK vượt quá 25 ký tự).    |
| `04` | 400  | **Không tìm thấy ngân hàng** trong mạng lưới NAPAS.                           |
| `05` | 400  | **Template không hợp lệ** (chỉ chấp nhận `compact`, `qr_only`, `print`).      |
| `99` | 500  | **Lỗi ngoại lệ hệ thống**.                                                    |

---

## 🧪 Kiểm Thử (Tests & Benchmarks)

Dự án bao gồm bộ kiểm thử toàn diện với hơn 530 test cases:

```bash
# Chạy toàn bộ test suite
dotnet test

# Chạy riêng nhóm kiểm thử chuẩn EMVCo NAPAS247
dotnet test --filter "FullyQualifiedName~NapasVietQrStandardTests"

# Chạy benchmark hiệu năng thuật toán CRC và SkiaSharp render
dotnet test --filter "FullyQualifiedName~Benchmark"
```

---

## 📚 Tài Liệu Kỹ Thuật Đính Kèm

Toàn bộ tài liệu chi tiết được lưu trữ tại thư mục [`docs/`](docs/):

- [**Quy định về Định dạng mã VietQR (NAPAS v1.0 PDF)**](docs/QR_Format_T%26C_v1.0_VN_092021.pdf): Văn bản quy chuẩn chính thức từ NAPAS (09/2021).
- [**Báo Cáo Phân Tích & Đối Soát Hiệu Năng (PDF)**](docs/performance.pdf): Benchmark CRC Table-Driven vs Bit-by-Bit & bài toán 1.000 trường học.

---

## 📄 Quyền Sở Hữu & Giấy Phép

- **Nhãn hiệu:** `VietQR`, `NAPAS`, `NAPAS247` thuộc toàn quyền sở hữu của **Công ty Cổ phần Thanh toán Quốc gia Việt Nam (NAPAS)**.
- **Tiêu chuẩn:** Tuân thủ chuẩn QR của **Ngân hàng Nhà nước Việt Nam** và chuẩn quốc tế **EMVCo**.
- **Mã nguồn:** Cung cấp theo giấy phép [MIT License](LICENSE). Xem chi tiết tại [SECURITY.md](SECURITY.md) và [CONTRIBUTING.md](CONTRIBUTING.md).
