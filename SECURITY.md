# CHÍNH SÁCH BẢO MẬT (SECURITY POLICY)

Dự án `vietqr-core` xử lý các thông tin khởi tạo giao dịch thanh toán tài chính và ngân hàng, do đó an toàn thông tin và tính bảo mật luôn là ưu tiên hàng đầu.

## 1. Các Phiên Bản Được Hỗ Trợ

| Phiên bản |        Trạng thái hỗ trợ        | Nền tảng Runtime     |
| :-------- | :-----------------------------: | :------------------- |
| `1.x`     | ✅ Được hỗ trợ cập nhật bảo mật | .NET 8.0 LTS trở lên |
| `< 1.0`   |         ❌ Ngừng hỗ trợ         | .NET Core cũ         |

## 2. Các Biện Pháp Bảo Mật Đã Triển Khai

- **Khử ký tự điều khiển ẩn & SQLi / XSS:** Hệ thống tự động lọc và chuẩn hóa chuỗi `description`, loại bỏ ký tự điều khiển UTF-8 lạ trước khi nạp vào chuỗi EMVCo và render đồ họa.
- **Giới hạn số tiền & tài khoản:** Kiểm tra chặt chẽ định dạng độ dài tài khoản (tối đa 25 ký tự theo chuẩn NAPAS / Virtual Account), mã BIN (đúng 6 chữ số), và số tiền (phải là số nguyên dương hợp lệ).
- **Offline-First Isolation:** Không thực hiện bất kỳ lệnh gọi HTTP ra ngoài trong luồng request tạo mã QR của người dùng, loại bỏ nguy cơ rò rỉ dữ liệu qua bên thứ ba và ngăn ngừa tấn công SSRF / Man-in-the-Middle.
- **Quyền ghi tối thiểu:** Thư mục `wwwroot/data` chỉ cần quyền ghi cục bộ cho Background Service cập nhật file danh mục ngân hàng định kỳ, không mở quyền thực thi mã ngoài (`no-exec`).

## 3. Báo Cáo Lỗ Hổng Bảo Mật (Vulnerability Reporting)

Nếu bạn phát hiện bất kỳ lỗ hổng bảo mật nào trong dự án, xin vui lòng **KHÔNG TẠO ISSUE CÔNG KHAI TRÊN GITHUB**. Thay vào đó, hãy thông báo bảo mật theo quy trình sau:

1. **Gửi báo cáo qua kênh riêng tư:**
    - Sử dụng tính năng [GitHub Private Vulnerability Reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-communicating-vulnerabilities/privately-reporting-a-security-vulnerability) tại tab **Security** của repository.
    - Hoặc gửi email trực tiếp tới người quản trị dự án kèm tiêu đề: `[SECURITY] Phát hiện lỗ hổng trong VietQR Core`.
2. **Nội dung báo cáo nên bao gồm:**
    - Mô tả chi tiết bản chất lỗ hổng (Buffer overflow, ReDoS, Logic bypass, Memory leak...).
    - Các bước tái hiện (Proof of Concept - PoC) hoặc mẫu request HTTP / chuỗi đầu vào gây lỗi.
    - Đánh giá mức độ ảnh hưởng (Nghiêm trọng / Trung bình / Thấp).
    - Đề xuất giải pháp khắc phục (nếu có).
