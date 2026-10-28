# HƯỚNG DẪN ĐÓNG GÓP MÃ NGUỒN (CONTRIBUTING GUIDELINES)

Cảm ơn bạn đã quan tâm và muốn đóng góp cho dự án `vietqr-core`! Dưới đây là quy chuẩn và các bước hướng dẫn để quy trình cộng tác diễn ra trơn tru, chất lượng.

---

## 1. Quy Trình Làm Việc (Workflow)

1. **Fork hoặc tạo Branch mới:**
   - Tạo nhánh tính năng/sửa lỗi từ nhánh `main`:
     ```bash
     git checkout -b feature/ten-tinh-nang
     # hoặc
     git checkout -b fix/loi-can-sua
     ```
2. **Cài đặt & Build kiểm tra:**
   - Dự án yêu cầu **.NET 8.0 SDK** trở lên.
   - Biên dịch kiểm tra:
     ```bash
     dotnet restore
     dotnet build
     ```
3. **Thực hiện thay đổi:**
   - Viết mã nguồn sạch, có chú thích XML docs cho các public methods / controllers.
   - Thêm unit test tương ứng nếu bổ sung tính năng mới hoặc sửa lỗi logic.
4. **Chạy kiểm thử trước khi Commit:**
   - Đảm bảo toàn bộ test case kiểm thử chuẩn NAPAS đều vượt qua:
     ```bash
     dotnet test
     ```
5. **Tạo Pull Request (PR):**
   - Mô tả rõ ràng lý do thay đổi, giải pháp kỹ thuật và kết quả test đã thực hiện.

---

## 2. Tiêu Chuẩn Viết Code (Coding Standards)

- **C# 12 & .NET 8:**
  - Tận dụng các tính năng mới của C# 12: Primary Constructors, Collection Expressions (`[ .. ]`), Pattern Matching (`switch expressions`).
  - Kích hoạt `Nullable` reference types (`<Nullable>enable</Nullable>`).
- **Hiệu Năng & Zero Allocation:**
  - Đối với các hàm xử lý chuỗi và tính toán (như CRC, TLV parser), ưu tiên sử dụng `ReadOnlySpan<char>`, `ReadOnlySpan<byte>` thay vì cấp phát `new string` hoặc `new byte[]` thừa thãi.
  - Tối ưu hóa bộ nhớ heap để không gây áp lực lên Garbage Collector (GC) trong các kịch bản chịu tải cao.
- **Xử lý Đồ Hoạ SkiaSharp:**
  - Luôn bọc các đối tượng đồ họa không quản lý (`SKBitmap`, `SKCanvas`, `SKPaint`, `SKImage`, `SKData`) trong khối `using` hoặc `IDisposable` để giải phóng bộ nhớ native lập tức.

---

## 3. Quy Ước Commit (Commit Conventions)

Dự án áp dụng quy chuẩn **Conventional Commits**:
- `feat:` Thêm chức năng, template hoặc endpoint mới.
- `fix:` Sửa lỗi logic, sửa lỗi render hoặc tính sai CRC/TLV.
- `perf:` Cải thiện hiệu năng xử lý, giảm thời gian render ảnh.
- `test:` Bổ sung hoặc cập nhật unit test, benchmark tests.
- `docs:` Cập nhật tài liệu README, API docs, hướng dẫn cài đặt.
- `refactor:` Tái cấu trúc mã nguồn mà không làm thay đổi hành vi nghiệp vụ.
- `chore:` Nâng cấp gói NuGet, cấu hình CI/CD, cập nhật `.gitignore`.

*Ví dụ:*
```bash
git commit -m "feat: bổ sung tính năng auto-fit font chữ cho template print"
git commit -m "perf: tối ưu thuật toán CRC16-CCITT sử dụng ReadOnlySpan"
```

---

## 4. Kiểm Thử & Đo Đạc (Testing)

Mọi đóng góp liên quan đến thuật toán chuẩn hóa chuỗi EMVCo bắt buộc phải đối chiếu với tài liệu quy chuẩn kỹ thuật NAPAS v1.0 trong thư mục `docs/`. Tuyệt đối không đưa vào các thay đổi làm sai lệch cấu trúc trường TLV hoặc CRC checksum.
