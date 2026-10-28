# VietQR Core API (NAPAS247 Microservice)

<p align="center">
  <img src="wwwroot/data/vietqr.png" alt="VietQR Logo" width="180" />
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="wwwroot/data/napas247.png" alt="NAPAS247 Logo" width="180" />
</p>

<p align="center">
  <b>Dịch vụ vi mô hiệu năng cao tạo chuỗi & render ảnh mã thanh toán VietQR chuẩn NAPAS247</b><br>
  Xây dựng trên nền tảng <b>.NET 8 (C# 12)</b> & đồ hoạ vector <b>SkiaSharp 4.x</b><br>
  Hỗ trợ vận hành độc lập (Offline-First), tự động đồng bộ danh mục ngân hàng và tối ưu cấp phát bộ nhớ (Zero Allocation).
</p>

<p align="center">
  <a href="#"><img src="https://img.shields.io/badge/.NET_Build_%26_Test-passing-brightgreen?style=flat-square&logo=dotnet&logoColor=white" alt=".NET Build & Test" /></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-8.0_LTS-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8.0 LTS" /></a>
  <a href="#"><img src="https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp&logoColor=white" alt="C# 12" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Standard-NAPAS247_EMVCo-005BAA?style=flat-square" alt="Standard NAPAS247" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Graphics-SkiaSharp_4.x-E65100?style=flat-square" alt="SkiaSharp 4.x" /></a>
  <br>
  <a href="#"><img src="https://img.shields.io/badge/Tests-500%2B_passed-2ea44f?style=flat-square&logo=githubactions&logoColor=white" alt="Tests 500+ Passed" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Architecture-Offline--First_Microservice-7928CA?style=flat-square" alt="Offline-First Microservice" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-0366d6?style=flat-square" alt="License MIT" /></a>
  <a href="SECURITY.md"><img src="https://img.shields.io/badge/Security-Policy_Active-black?style=flat-square&logo=securityscorecard&logoColor=white" alt="Security Active" /></a>
  <a href="CONTRIBUTING.md"><img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square" alt="PRs Welcome" /></a>
</p>

---

## 📑 Mục Lục
1. [Giới Thiệu Tổng Quan](#-giới-thiệu-tổng-quan)
2. [Tính Năng Nổi Bật](#-tính-năng-nổi-bật)
3. [Công Nghệ & Thư Viện Sử Dụng](#-công-nghệ--thư-viện-sử-dụng)
4. [Cấu Trúc Thư Mục](#-cấu-trúc-thư-mục)
5. [3 Mẫu Template Thẻ QR](#-3-mẫu-template-thẻ-qr)
6. [Tài Liệu Chi Tiết API](#-tài-liệu-chi-tiết-api)
7. [Cơ Chế Offline-First & Đồng Bộ Ngân Hàng](#-cơ-chế-offline-first--đồng-bộ-ngân-hàng)
8. [Hướng Dẫn Cài Đặt & Khởi Chạy](#-hướng-dẫn-cài-đặt--khởi-chạy)
9. [Kiểm Thử & Đo Đạc Hiệu Năng (Testing & Benchmarks)](#-kiểm-thử--đo-đạc-hiệu-năng-testing--benchmarks)
10. [Hướng Dẫn Triển Khai Production (IIS / Docker)](#-hướng-dẫn-triển-khai-production-iis--docker)
11. [Danh Sách Mã Lỗi (Response Codes)](#-danh-sách-mã-lỗi-response-codes)
12. [Quyền Sở Hữu Trí Tuệ & Điều Khoản Sử Dụng (T&C)](#-quyền-sở-hữu-trí-tuệ--điều-khoản-sử-dụng-tc)
13. [Tài Liệu Kỹ Thuật & Báo Cáo Đi Kèm](#-tài-liệu-kỹ-thuật--báo-cáo-đi-kèm)

---

## 🌟 Giới Thiệu Tổng Quan

`vietqr-core` là giải pháp microservice độc lập, chuyên sâu cho bài toán tạo mã thanh toán QR theo chuẩn quốc gia **NAPAS247 / VietQR (EMVCo)**. 

Dự án loại bỏ hoàn toàn sự phụ thuộc thời gian thực vào các bên trung gian thứ ba:
- **Tốc độ cực nhanh:** Sinh chuỗi EMVCo chỉ mất **~0.0001 ms / mã** với thuật toán CRC16-CCITT Table-Driven không cấp phát bộ nhớ Heap rác.
- **Render đồ họa sắc nét:** Tích hợp bộ giải thuật SkiaSharp 4.x với phép nội suy `Mitchell` và cơ chế `GetContentBounds` tự động căn chỉnh và khử viền thừa logo ngân hàng.
- **Sẵn sàng cho tải lớn:** Đáp ứng hàng triệu yêu cầu xuất phiếu thu học phí, hoá đơn điện tử, đơn hàng thương mại điện tử trong thời gian ngắn mà không gây nghẽn Garbage Collector (GC).

---

## 🚀 Tính Năng Nổi Bật

- **Chuẩn hóa EMVCo NAPAS247:** Hỗ trợ cả 2 phương thức chuyển khoản nhanh:
  - Chuyển nhanh qua số tài khoản (`QRIBFTTA` - Service code `02`).
  - Chuyển nhanh qua số thẻ (`QRIBFTTC` - Service code `03`).
- **Thuật toán CRC16-CCITT Branchless:** Dùng bảng tra trước nhúng trực tiếp `ReadOnlySpan<ushort>` trong C# 12, zero-allocation bộ nhớ.
- **3 Mẫu Template hiển thị linh hoạt:**
  - `compact`: Mẫu thẻ ảnh mặc định (Header VietQR + Khung thẻ + QR Code + Footer Napas247 & logo ngân hàng).
  - `qr_only`: Mẫu tinh gọn 500x500 px chỉ chứa mã QR kèm logo VietQR ở tâm, tối ưu cho App di động, Website.
  - `print`: Mẫu in ấn mở rộng 600x745 px hiển thị đầy đủ Số tiền, Nội dung CK, Tên chủ TK, Số TK (in đậm) và Tên ngân hàng kèm thuật toán Auto-fit co giãn font chữ tự động chống tràn viền.
- **Cơ chế Offline-First & Multi-Tier Caching:**
  - Hoạt động ổn định ngay cả khi không có kết nối internet ra ngoài.
  - Bộ nhớ RAM (`ConcurrentDictionary`) kết hợp bộ nhớ đĩa cứng (`wwwroot/data/banks.json` và `wwwroot/data/logos/`).
  - Background Service tự động cập nhật danh mục 76+ ngân hàng mới nhất lúc 1h sáng hàng ngày (GMT+7).
- **Chẩn đoán hạ tầng & Quản trị:**
  - Endpoint `/test-rw` tự động kiểm tra quyền Đọc / Ghi / Xóa file thực tế trên phân vùng máy chủ (đặc biệt hữu ích khi thiết lập quyền IIS hoặc Linux Docker).
  - Trang thư viện trực quan xem trước 76 ngân hàng (`/gallery`).
  - Tích hợp tài liệu OpenAPI / Swagger UI chi tiết.

---

## 🛠 Công Nghệ & Thư Viện Sử Dụng

- **Framework:** .NET 8.0 (C# 12) Web API
- **QR Code Engine:** `QRCoder (1.8.1)`
- **Graphics Engine:** `SkiaSharp (4.153.1)` & Native Assets cho Windows/macOS/Linux
- **Documentation:** `Swashbuckle.AspNetCore (6.6.2)` với XML Docs đầy đủ
- **Testing:** `xUnit` với hơn 500+ test cases kiểm thử chuẩn xác NAPAS, auto-fit text boundary, stress test và benchmark hiệu năng

---

## 📁 Cấu Trúc Thư Mục

```text
vietqr-core/
├── Controllers/
│   └── VietQrCoreController.cs         # API Controller chính (Generate, Banks, Sync, Benchmark...)
├── Models/
│   └── VietQrModels.cs                 # Data Transfer Objects (DTO) & Response Models
├── Services/
│   ├── VietQr.cs                       # Core logic sinh chuỗi EMVCo, TLV tags, CRC16-CCITT
│   ├── QrImageService.cs               # Engine render ảnh SkiaSharp (compact, qr_only, print)
│   └── BankSyncBackgroundService.cs    # Background Service đồng bộ 76+ ngân hàng định kỳ
├── Properties/
│   └── launchSettings.json             # Cấu hình môi trường chạy (Development, Ports...)
├── wwwroot/
│   ├── data/
│   │   ├── banks.json                  # Dữ liệu danh mục 76+ ngân hàng lưu đĩa cứng
│   │   ├── vietqr.png                  # Logo VietQR header
│   │   ├── vietqr_icon.png             # Logo VietQR tâm mã QR
│   │   ├── napas247.png                # Logo NAPAS247 footer
│   │   └── logos/                      # Thư mục lưu trữ logo 76 ngân hàng theo mã BIN
│   └── all_banks_gallery.html          # File HTML xem trước 76 ngân hàng (được sinh khi chạy test)
├── docs/                               # Tài liệu tiêu chuẩn VietQR, báo cáo hiệu năng & test case
├── tests/
│   └── vietqr-core.Tests/              # Bộ kiểm thử xUnit chuẩn NAPAS & Benchmarks
├── Program.cs                          # Entry point cấu hình DI, Swagger, CORS, Hosted Service
├── appsettings.json                    # Cấu hình ứng dụng
├── web.config                          # Cấu hình IIS Hosting
├── vietqr-core.csproj                  # Project file .NET 8
└── vietqr-core.http                    # File mẫu gửi request trực tiếp trên IDE
```

---

## 🖼 3 Mẫu Template Thẻ QR

| Template | Kích thước | Mô tả | Ứng dụng phù hợp |
| :--- | :---: | :--- | :--- |
| `compact` *(Mặc định)* | **600 × 585 px** | Thẻ thanh toán hoàn chỉnh: Header VietQR, Khung thẻ bo góc, Mã QR có tâm VietQR, Footer thương hiệu NAPAS247 và Logo ngân hàng thụ hưởng. | Quét mã tại quầy thu ngân, hiển thị trên website, trang checkout thanh toán. |
| `qr_only` | **500 × 500 px** | Ảnh mã QR vuông vức tinh gọn, chỉ gồm mã QR độ phân giải cao kèm logo VietQR ở trung tâm. | Nhúng vào Modal Popup, màn hình ứng dụng di động (Mobile App), hệ thống POS. |
| `print` | **600 × 745 px** | Mẫu in ấn chuyên dụng mở rộng: Header VietQR, Mã QR lớn, Khung thông tin thanh toán (Số tiền định dạng VNĐ, Nội dung CK, Tên chủ TK, Số TK in đậm, Tên ngân hàng). Có thuật toán tự giảm cỡ chữ nếu nội dung dài. | In hoá đơn, phiếu thu học phí, biên lai thanh toán giấy A4/A5, thông báo nộp tiền. |

---

## 📡 Tài Liệu Chi Tiết API

Prefix URL mặc định: `http://localhost:5187/api/v1/vietqr-core`

### 1. Tạo Mã Thanh Toán VietQR
- **Endpoint:** `GET /api/v1/vietqr-core/generate`
- **Mô tả:** Sinh chuỗi chuẩn EMVCo và render ảnh thẻ thanh toán (Base64 PNG).

#### Các tham số Query Parameters:
| Tham số | Bắt buộc | Kiểu | Mô tả | Ví dụ |
| :--- | :---: | :---: | :--- | :--- |
| `bankBin` | **Có** | `string` | Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số). | `970422` (MBBank), `970436` (Vietcombank) |
| `accountNo` | **Có** | `string` | Số tài khoản hoặc số thẻ người thụ hưởng (tối đa 19 ký tự). | `056789799999` |
| `amount` | **Có** | `string` | Số tiền thanh toán (VNĐ), phải là số nguyên dương > 0. | `50000` |
| `description` | **Có** | `string` | Nội dung chuyển khoản (hệ thống tự chuẩn hoá, khử dấu tiếng Việt). | `thanh toan hoc phi` |
| `accountName`| Không | `string` | Tên chủ tài khoản người thụ hưởng (dùng hiển thị trên mẫu in ấn `print`). | `NGUYEN VAN A` |
| `template` | Không | `string` | Mẫu giao diện xuất ra: `compact` (mặc định), `qr_only`, hoặc `print`. | `compact` |

#### Ví dụ Request:
```http
GET /api/v1/vietqr-core/generate?bankBin=970422&accountNo=056789799999&amount=50000&description=thanh toan hoc phi&template=compact
Accept: application/json
```

#### Phản hồi Thành Công (HTTP 200):
```json
{
  "code": "00",
  "desc": "Tạo mã VietQR và render ảnh thành công",
  "data": {
    "qrCode": "00020101021238540010A0000007270124000697042201120567897999990208QRIBFTTA53037045405500005802VN62230819thanh toan hoc phi6304E1F8",
    "qrDataURL": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAlgAAAJJAQMAAACu6UvVAAAABlBMVEX///...",
    "template": "compact",
    "bankInfo": {
      "bin": "970422",
      "code": "MB",
      "shortName": "MBBank",
      "name": "Ngân hàng TMCP Quân đội"
    }
  }
}
```

---

### 2. Lấy Danh Sách Ngân Hàng
- **Endpoint:** `GET /api/v1/vietqr-core/banks`
- **Mô tả:** Lấy toàn bộ danh mục 76+ ngân hàng hỗ trợ chuẩn chuyển tiền nhanh NAPAS247 kèm mã BIN, tên viết tắt, tên đầy đủ và trạng thái logo.

---

### 3. Kiểm Tra Trạng Thái Đồng Bộ Ngầm
- **Endpoint:** `GET /api/v1/vietqr-core/sync-status`
- **Mô tả:** Xem thông tin lần đồng bộ gần nhất: thời gian (giờ VN), số lượng ngân hàng, số lượng logo đã tải, thời gian dự kiến chạy lần tiếp theo (1h sáng).

---

### 4. Kích Hoạt Đồng Bộ Thủ Công Ngay Lập Tức
- **Endpoint:** `POST /api/v1/vietqr-core/sync-now`
- **Mô tả:** Buộc hệ thống gọi lên máy chủ VietQR IO để cập nhật lại file `banks.json` và tải bổ sung các logo mới về `wwwroot/data/logos/`.

---

### 5. Kiểm Tra Quyền Đọc - Ghi - Xoá Phân Vùng Đĩa (Self Diagnostic)
- **Endpoint:** `GET /api/v1/vietqr-core/test-rw`
- **Mô tả:** Tạo thử một file tạm, ghi dữ liệu, đọc lại kiểm tra nội dung và xoá file trong thư mục `wwwroot/data`. Dùng để kiểm tra quyền của tài khoản ứng dụng (ví dụ: `IIS_IUSRS`, `NETWORK SERVICE`, hoặc Docker user).

---

### 6. Xem Thư Viện Thẻ QR 76 Ngân Hàng
- **Endpoint:** `GET /api/v1/vietqr-core/gallery`
- **Mô tả:** Chuyển hướng trực tiếp đến trang `all_banks_gallery.html` hiển thị toàn bộ 76 mã QR đã render để kiểm thử quét bằng ứng dụng ngân hàng.

---

### 7. Chạy Thử Nghiệm Tải & Stress Test Quy Mô Lớn
- **Endpoint:** `GET /api/v1/vietqr-core/benchmark-school-test`
- **Tham số:**
  - `testRequests`: Số request chạy thử (mặc định: `2000`).
  - `template`: Mẫu template đo đạc (`qr_only`, `compact`, `print`, `emv_only`).
  - `totalSchools`: Quy mô số trường học (mặc định: `1000`).
  - `studentsPerSchool`: Số học sinh mỗi trường (mặc định: `1000`).
- **Mô tả:** Chạy đa luồng song song, đo lường tốc độ xử lý thực tế (RPS), độ trễ trung bình, mức tiêu thụ RAM và đưa ra bài toán dự phóng thời gian xuất phiếu thu thực tế.

---

## 🔒 Cơ Chế Offline-First & Đồng Bộ Ngân Hàng

1. **Khởi động ứng dụng:**
   - Hệ thống tự động nạp dữ liệu từ file đĩa cứng `wwwroot/data/banks.json` vào bộ nhớ RAM `ConcurrentDictionary`.
   - Các logo có sẵn trong thư mục `wwwroot/data/logos/` được nạp sẵn sàng phục vụ render.
2. **Khi có Request tạo mã:**
   - Tra cứu ngân hàng trực tiếp từ RAM trong thời gian $O(1)$ (< 0.001 ms).
   - Render đồ họa trực tiếp tại chỗ bằng thư viện SkiaSharp nhúng.
   - **Tuyệt đối không gửi request ra internet**, đảm bảo độ trễ phản hồi thấp nhất và an toàn khi mất kết nối mạng.
3. **Tiến trình chạy ngầm định kỳ:**
   - `BankSyncBackgroundService` tính toán thời gian đến 01:00 AM sáng hôm sau (Múi giờ Việt Nam - `SE Asia Standard Time`).
   - Tự động tải bản tin mới từ `https://api.vietqr.io/v2/banks`.
   - Ghi đè an toàn vào `banks.json` và tải logo bổ sung về `logos/`.

---

## 💻 Hướng Dẫn Cài Đặt & Khởi Chạy

### Yêu Cầu Môi Trường
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trở lên.
- Trình soạn thảo: Visual Studio 2022 / VS Code / Rider.

### Các Bước Thực Hiện
1. **Clone dự án về máy:**
   ```bash
   git clone <repository_url>
   cd vietqr-core/vietqr-core
   ```

2. **Restore gói thư viện NuGet:**
   ```bash
   dotnet restore
   ```

3. **Chạy ứng dụng:**
   ```bash
   dotnet run
   ```

4. **Truy cập tài liệu Swagger UI:**
   Mở trình duyệt tại địa chỉ:
   ```text
   http://localhost:5187/swagger
   ```

5. **Gửi Request kiểm tra nhanh bằng file `.http`:**
   Mở file [`vietqr-core.http`](vietqr-core.http) trong VS Code / Visual Studio để gửi trực tiếp các kịch bản test có sẵn.

---

## 🧪 Kiểm Thử & Đo Đạc Hiệu Năng (Testing & Benchmarks)

Dự án tích hợp bộ kiểm thử xUnit bao quát toàn diện tại `tests/vietqr-core.Tests`:

### 1. Chạy Bộ Kiểm Thử Chuẩn NAPAS247 (500+ Test Cases)
```bash
dotnet test tests/vietqr-core.Tests/vietqr_core.Tests.csproj --filter "FullyQualifiedName~NapasVietQrStandardTests"
```
*Kết quả:* Kiểm tra tính hợp lệ của mã chuỗi EMVCo, độ dài các trường TLV, kiểm tra mã CRC16-CCITT theo đúng tiêu chuẩn thông tư Ngân hàng Nhà nước & NAPAS.

### 2. Sinh File Thư Viện Ảnh 76 Ngân Hàng (Gallery)
```bash
dotnet test tests/vietqr-core.Tests/vietqr_core.Tests.csproj --filter "GenerateAll76BanksPrintCards"
```
Lệnh này sẽ tự động sinh file `tests/all_banks_gallery.html`. Bạn có thể mở trực tiếp để kiểm tra quét mã bằng camera điện thoại:
```bash
# Trên macOS:
open tests/all_banks_gallery.html

# Trên Windows:
start tests/all_banks_gallery.html
```

### 3. Hiệu Năng Đo Đạc Thực Tế
- **Sinh chuỗi EMVCo (VietQr.cs):** **~10.000.000+ mã / giây** (Zero Heap Allocation nhờ `ReadOnlySpan<char>` và bảng tra tĩnh 256 phần tử).
- **Render ảnh `qr_only` (500x500):** **~3 - 5 ms / ảnh**.
- **Render ảnh `compact` (600x585):** **~6 - 10 ms / ảnh**.
- **Render ảnh `print` (600x745):** **~8 - 14 ms / ảnh**.

---

## 🌐 Hướng Dẫn Triển Khai Production (IIS / Docker)

### 1. Triển Khai Trên Máy Chủ Windows IIS
1. **Cài đặt hosting bundle:** Cài đặt **.NET 8 Hosting Bundle** trên Windows Server.
2. **Publish ứng dụng:**
   ```bash
   dotnet publish -c Release -o C:\inetpub\wwwroot\vietqr-core
   ```
3. **Cấu hình Application Pool:**
   - .NET CLR Version: `No Managed Code`.
   - Managed Pipeline Mode: `Integrated`.
4. **Phân quyền thư mục `wwwroot/data` (Bắt buộc):**
   - Do Background Service cần ghi đè file `banks.json` và tải logo, thư mục `wwwroot/data` cần được cấp quyền `Modify / Write` cho người dùng:
     - `IIS_IUSRS`
     - `IUSR` (nếu dùng Anonymous Authentication)
     - Hoặc Identity của Application Pool (`IIS AppPool\<TênAppPool>`).
5. **Kiểm tra quyền:** Sau khi cấu hình, gọi endpoint `/api/v1/vietqr-core/test-rw` để đảm bảo hệ thống báo `canWrite: true` và `canDelete: true`.

---

## 📋 Danh Sách Mã Lỗi (Response Codes)

Mọi API đều trả về định dạng chuẩn có trường `code` và `desc`:

| Mã Code | Trạng Thái HTTP | Ý Nghĩa | Nguyên Nhân & Cách Khắc Phục |
| :---: | :---: | :--- | :--- |
| `00` | 200 OK | **Thành công** | Yêu cầu được xử lý trọn vẹn, dữ liệu trả về trong thuộc tính `data`. |
| `01` | 400 Bad Request | **Thiếu tham số bắt buộc** | Thiếu một trong các tham số: `bankBin`, `accountNo`, `amount`, `description`. |
| `02` | 400 Bad Request | **Số tiền không hợp lệ** | Tham số `amount` không phải là số hoặc giá trị $\le 0$ VNĐ. |
| `03` | 400 Bad Request | **Mã BIN sai định dạng** | `bankBin` phải đúng 6 chữ số (Ví dụ: `970422`, không truyền ký tự chữ như `MB`). |
| `04` | 400 Bad Request | **Không tìm thấy ngân hàng** | Mã BIN hợp lệ 6 số nhưng không nằm trong danh mục ngân hàng thành viên NAPAS. |
| `05` | 400 Bad Request | **Mẫu Template không hợp lệ** | Template truyền vào khác `compact`, `qr_only`, hoặc `print`. |
| `99` | 500 Internal Error | **Lỗi ngoại lệ hệ thống** | Lỗi bất ngờ trong quá trình sinh mã đồ họa hoặc xử lý hệ thống. |

---

## 📄 Quyền Sở Hữu Trí Tuệ & Điều Khoản Sử Dụng (T&C)

Căn cứ theo **Tài liệu Quy định về Định dạng mã VietQR trong Dịch vụ chuyển nhanh NAPAS247 (Phiên bản 1.0 - 09/2021)** do **Công ty Cổ phần Thanh toán Quốc gia Việt Nam (NAPAS)** ban hành:

### 1. Quyền Sở Hữu Nhãn Hiệu & Tiêu Chuẩn Kỹ Thuật
- **Nhãn hiệu VietQR & NAPAS247:** Nhãn hiệu thương mại, hình ảnh logo và nhận diện thương hiệu `VietQR`, `NAPAS`, `NAPAS247` thuộc toàn quyền sở hữu của **Công ty Cổ phần Thanh toán Quốc gia Việt Nam (NAPAS)**.
- **Chuẩn kỹ thuật EMVCo & NHNN:** Cấu trúc định dạng mã thanh toán tuân thủ theo **Tiêu chuẩn cơ sở cho mã QR của Ngân hàng Nhà nước Việt Nam** và chuẩn thanh toán quốc tế **EMVCo Merchant-Presented Mode**.
- **Cam kết tuân thủ T&C NAPAS:** Dự án tuân thủ đầy đủ các điều khoản tại Mục 8 trong tài liệu quy định của NAPAS:
  - Không có bất kỳ hành động nào thể hiện hoặc ngầm ý xác lập quyền sở hữu đối với tài liệu đặc tả kỹ thuật, nhãn hiệu thương mại hoặc sở hữu trí tuệ của NAPAS, NHNN và EMVCo.
  - Sử dụng chuẩn định dạng để xây dựng công cụ tạo mã VietQR phục vụ thanh toán hợp pháp qua mạng lưới Dịch vụ Chuyển nhanh NAPAS247.

### 2. Triển Khai Kỹ Thuật & Trách Nhiệm Dữ Liệu
- **Mã nguồn giải pháp:** Dự án `vietqr-core` là bộ công cụ vi mô (microservice) độc lập, cung cấp giải pháp kỹ thuật tạo chuỗi EMVCo và render đồ hoạ thẻ thanh toán.
- **Bảo mật & Quản lý thông tin:** Bên triển khai và vận hành chịu hoàn toàn trách nhiệm đối với việc quản lý, bảo mật thông tin thanh toán (Số tài khoản, Mã ngân hàng, Số tiền, Nội dung giao dịch) và tuân thủ các quy định hiện hành của Ngân hàng Nhà nước Việt Nam về an toàn thanh toán điện tử.

---

## 📚 Tài Liệu Kỹ Thuật & Báo Cáo Đi Kèm

Các tài liệu nghiên cứu, quy chuẩn kỹ thuật và báo cáo hiệu năng chính thức được lưu trữ trực tiếp trong thư mục [`docs/`](docs/):

| STT | Tài Liệu | Định Dạng | Mô Tả Chi Tiết |
| :---: | :--- | :---: | :--- |
| 1 | [**Quy định về Định dạng mã VietQR (NAPAS v1.0)**](docs/QR_Format_T%26C_v1.0_VN_092021.pdf) | `PDF` | Văn bản quy chuẩn chính thức từ Công ty Cổ phần Thanh toán Quốc gia Việt Nam (NAPAS) ban hành tháng 09/2021, quy định cấu trúc TLV, ID dữ liệu và Điều khoản & Điều kiện (T&C). |
| 2 | [**Báo Cáo Phân Tích & Đối Soát Hiệu Năng Tổng Thể**](docs/performance.pdf) | `PDF` | Bảng tổng hợp thực nghiệm so sánh thuật toán CRC16-CCITT (Table-Driven vs Bit-by-Bit), kiểm thử 3 mẫu giao diện và mô phỏng tải bài toán 1.000 trường học (1.000.000 học sinh). |
| 3 | [**Hướng Dẫn Chạy Kiểm Thử & Benchmark CLI**](docs/huong-dan-chay-test.txt) | `TXT` | Tổng hợp câu lệnh Terminal/CLI để chạy test case NAPAS, benchmark thời gian thực và sinh thư viện 76 ngân hàng xem trước. |
