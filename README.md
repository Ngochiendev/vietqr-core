# VietQR Core API (NAPAS247 Microservice)

<p align="center">
  <img src="wwwroot/data/vietqr.png" alt="VietQR Logo" width="160" />
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="wwwroot/data/napas247.png" alt="NAPAS247 Logo" width="160" />
</p>

<p align="center">
  <b>Dịch vụ vi mô hiệu năng cao tạo chuỗi & render ảnh mã thanh toán VietQR chuẩn NAPAS247</b><br>
  .NET 8 (C# 12) &bull; SkiaSharp 4.x &bull; Offline-First &bull; Zero Heap Allocation
</p>

<p align="center">
  <a href="#"><img src="https://img.shields.io/badge/.NET_Build_%26_Test-passing-brightgreen?style=flat-square&logo=dotnet&logoColor=white" alt=".NET Build" /></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-8.0_LTS-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8.0" /></a>
  <a href="#"><img src="https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp&logoColor=white" alt="C# 12" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Standard-NAPAS247_EMVCo-005BAA?style=flat-square" alt="Standard NAPAS247" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Graphics-SkiaSharp_4.x-E65100?style=flat-square" alt="SkiaSharp 4.x" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Tests-500%2B_passed-2ea44f?style=flat-square&logo=githubactions&logoColor=white" alt="Tests Passed" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-0366d6?style=flat-square" alt="License MIT" /></a>
  <a href="SECURITY.md"><img src="https://img.shields.io/badge/Security-Active-black?style=flat-square&logo=securityscorecard&logoColor=white" alt="Security" /></a>
  <a href="CONTRIBUTING.md"><img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square" alt="PRs Welcome" /></a>
</p>

---

## 🖼 Bảng Ảnh Mẫu 3 Template VietQR

Hệ thống hỗ trợ 3 mẫu giao diện xuất ra (Base64 PNG) được lưu tại [`docs/image/`](docs/image):

|                        `qr_only` (500 × 500 px)                         |                    `compact` (600 × 585 px) - _Mặc định_                    |                                              `print` (600 × 745 px)                                               |
| :---------------------------------------------------------------------: | :-------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------------------: |
| <img src="docs/image/qr_only.png" alt="Template QR Only" width="220" /> |   <img src="docs/image/compact.png" alt="Template Compact" width="220" />   |                        <img src="docs/image/print.png" alt="Template Print" width="220" />                        |
|                            **Mẫu Tinh Gọn**                             |                           **Mẫu Thẻ Tiêu Chuẩn**                            |                                               **Mẫu In Ấn Mở Rộng**                                               |
|               Chỉ gồm mã QR vuông kèm logo VietQR ở tâm.                | Thẻ thanh toán đầy đủ Header VietQR, khung viền, logo NAPAS247 & Ngân hàng. | Hiển thị chi tiết: Số tiền (VNĐ), Nội dung CK, Tên chủ TK, Số TK in đậm, Tên NH. Tự co giãn font chống tràn viền. |
|            _Dành cho Mobile App, Popup Modal, Web Checkout._            |            _Dành cho POS, quầy thanh toán, trang xác nhận đơn._             |                             _Dành cho in hoá đơn, phiếu thu học phí, biên lai A4/A5._                             |

---

## ⚡ Điểm Nổi Bật

- **Chuẩn EMVCo NAPAS247:** Hỗ trợ chuyển nhanh qua tài khoản (`QRIBFTTA`) và thẻ (`QRIBFTTC`).
- **Tốc độ cực cao (~0.0001 ms/mã):** Thuật toán CRC16-CCITT Table-Driven nhúng `ReadOnlySpan<ushort>`, **0 byte rác Heap**.
- **Offline-First 100%:** Sinh mã hoàn toàn độc lập không gọi internet thời gian thực.
- **Tự động đồng bộ 76+ Ngân hàng:** Background Service tự tải danh mục & logo lúc 01:00 AM hàng ngày từ VietQR IO vào bộ đệm RAM và ổ đĩa `wwwroot/data/`.
- **Đồ họa SkiaSharp 4.x:** Phép nội suy `Mitchell` sắc nét, tự nhận diện biên `GetContentBounds` khử viền thừa logo ngân hàng.

---

## 🚀 Khởi Chạy Nhanh

```bash
# 1. Khôi phục thư viện và biên dịch
dotnet restore
dotnet build

# 2. Chạy ứng dụng
dotnet run --project vietqr-core.csproj

# 3. Mở Swagger UI trên trình duyệt:
# http://localhost:5187/swagger
```

---

## 📡 API Tham Chiếu

Base URL: `http://localhost:5187/api/v1/vietqr-core`

### 1. Sinh Mã & Render Ảnh Thẻ

`GET /generate`

| Tham số       | Bắt buộc |   Kiểu   | Mô tả & Ví dụ                                                      |
| :------------ | :------: | :------: | :----------------------------------------------------------------- |
| `bankBin`     |  **Có**  | `string` | Mã BIN 6 số của ngân hàng (VD: `970422` - MB, `970436` - VCB).     |
| `accountNo`   |  **Có**  | `string` | Số tài khoản hoặc thẻ người thụ hưởng (tối đa 19 ký tự).           |
| `amount`      |  **Có**  | `string` | Số tiền VNĐ (> 0, VD: `50000`).                                    |
| `description` |  **Có**  | `string` | Nội dung chuyển khoản (tự khử dấu & ký tự lạ).                     |
| `accountName` |  Không   | `string` | Tên chủ tài khoản (hiển thị trên mẫu `print`, VD: `NGUYEN VAN A`). |
| `template`    |  Không   | `string` | Mẫu giao diện: `compact` (mặc định), `qr_only`, hoặc `print`.      |

**Ví dụ Request:**

```http
GET /api/v1/vietqr-core/generate?bankBin=970422&accountNo=056789799999&amount=50000&description=hoc%20phi&template=compact
```

**Phản hồi (HTTP 200):**

```json
{
  "code": "00",
  "desc": "Tạo mã VietQR và render ảnh thành công",
  "data": {
    "qrCode": "00020101021238540010A0000007270124000697042201120567897999990208QRIBFTTA53037045405500005802VN62110807hoc phi6304E6C2",
    "qrDataURL": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAlgAAAJJAQMAAACu6UvV...",
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

### 2. Các Endpoint Bổ Trợ Khác

- `GET /banks`: Lấy danh sách toàn bộ 76+ ngân hàng hỗ trợ.
- `GET /sync-status`: Xem thời gian và kết quả lần đồng bộ ngân hàng gần nhất.
- `POST /sync-now`: Kích hoạt đồng bộ danh mục ngân hàng & logo ngay lập tức.
- `GET /test-rw`: Kiểm tra quyền Đọc/Ghi/Xóa file trong thư mục `wwwroot/data` (hỗ trợ kiểm tra quyền IIS / Docker).
- `GET /gallery`: Xem trước thư viện ảnh của toàn bộ 76 ngân hàng (`all_banks_gallery.html`).

---

## 📋 Danh Sách Mã Trạng Thái

| Code | HTTP | Ý Nghĩa                                                                       |
| :--: | :--: | :---------------------------------------------------------------------------- |
| `00` | 200  | **Thành công** (kèm chuỗi `qrCode` và ảnh Base64 `qrDataURL`).                |
| `01` | 400  | **Thiếu tham số bắt buộc** (`bankBin`, `accountNo`, `amount`, `description`). |
| `02` | 400  | **Số tiền không hợp lệ** (phải là số nguyên dương > 0).                       |
| `03` | 400  | **Mã BIN sai định dạng** (phải đúng 6 chữ số).                                |
| `04` | 400  | **Không tìm thấy ngân hàng** trong mạng lưới NAPAS.                           |
| `05` | 400  | **Template không hợp lệ** (khác `compact`, `qr_only`, `print`).               |
| `99` | 500  | **Lỗi ngoại lệ hệ thống**.                                                    |

---

## 🧪 Kiểm Thử (Tests & Benchmarks)

```bash
# Chạy 500+ test case kiểm thử chuẩn NAPAS247
dotnet test tests/vietqr-core.Tests/vietqr_core.Tests.csproj --filter "FullyQualifiedName~NapasVietQrStandardTests"

# Sinh thư viện ảnh 76 ngân hàng xem trước (tests/all_banks_gallery.html)
dotnet test tests/vietqr-core.Tests/vietqr_core.Tests.csproj --filter "GenerateAll76BanksPrintCards"
```

---

## 📚 Tài Liệu Kỹ Thuật Đính Kèm

Toàn bộ tài liệu chi tiết được lưu trữ tại thư mục [`docs/`](docs/):

- [**Quy định về Định dạng mã VietQR (NAPAS v1.0 PDF)**](docs/QR_Format_T%26C_v1.0_VN_092021.pdf): Văn bản quy chuẩn chính thức từ NAPAS (09/2021).
- [**Báo Cáo Phân Tích & Đối Soát Hiệu Năng (PDF)**](docs/performance.pdf): Benchmark CRC Table-Driven vs Bit-by-Bit & bài toán 1.000 trường học.
- [**Hướng Dẫn Chạy Kiểm Thử & Benchmark CLI (TXT)**](docs/huong-dan-chay-test.txt): Lệnh Terminal chi tiết.

---

## 📄 Quyền Sở Hữu & Giấy Phép

- **Nhãn hiệu:** `VietQR`, `NAPAS`, `NAPAS247` thuộc toàn quyền sở hữu của **Công ty Cổ phần Thanh toán Quốc gia Việt Nam (NAPAS)**.
- **Tiêu chuẩn:** Tuân thủ chuẩn QR của **Ngân hàng Nhà nước Việt Nam** và chuẩn quốc tế **EMVCo**.
- **Mã nguồn:** Cung cấp theo giấy phép [MIT License](LICENSE). Xem chi tiết tại [DISCLAIMER.md](DISCLAIMER.md), [SECURITY.md](SECURITY.md) và [CONTRIBUTING.md](CONTRIBUTING.md).
