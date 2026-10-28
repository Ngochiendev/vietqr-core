using System.Text.Json.Serialization;

namespace vietqr_core.Models;

/// <summary>
/// Cấu trúc chuẩn phản hồi của API VietQR Core.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của thuộc tính data.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Mã trạng thái kết quả xử lý.
    /// <list type="bullet">
    /// <item><description><b>00</b>: Thành công.</description></item>
    /// <item><description><b>01</b>: Thiếu tham số bắt buộc (bankBin, accountNo, amount, description).</description></item>
    /// <item><description><b>02</b>: Số tiền không hợp lệ (nhỏ hơn hoặc bằng 0).</description></item>
    /// <item><description><b>03</b>: Mã BIN sai định dạng (bắt buộc đúng 6 chữ số, không dùng ký tự viết tắt).</description></item>
    /// <item><description><b>04</b>: Không tìm thấy thông tin ngân hàng trong danh mục NAPAS.</description></item>
    /// <item><description><b>05</b>: Template không hợp lệ (chỉ chấp nhận 'compact' hoặc 'qr_only').</description></item>
    /// <item><description><b>99</b>: Lỗi xử lý ngoại lệ hệ thống.</description></item>
    /// </list>
    /// </summary>
    /// <example>00</example>
    [JsonPropertyName("code")]
    public string Code { get; set; } = "00";

    /// <summary>
    /// Mô tả chi tiết kết quả hoặc thông điệp lỗi.
    /// </summary>
    /// <example>success</example>
    [JsonPropertyName("desc")]
    public string Desc { get; set; } = "success";

    /// <summary>
    /// Dữ liệu kết quả nghiệp vụ trả về khi thành công.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Dữ liệu sinh mã VietQR thành công.
/// </summary>
public class VietQrGenerateResult
{
    /// <summary>
    /// Chuỗi mã thanh toán VietQR theo chuẩn EMVCo (NAPAS247).
    /// </summary>
    /// <example>00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test63048E1D</example>
    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; } = string.Empty;

    /// <summary>
    /// Chuỗi Data URL ảnh Base64 định dạng image/png (dùng trực tiếp cho thẻ img trên Web hoặc Mobile App).
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAfQAAAH0CAYAAAD...</example>
    [JsonPropertyName("qrDataURL")]
    public string QrDataURL { get; set; } = string.Empty;
}

/// <summary>
/// Kết quả kiểm tra quyền đọc, ghi, xóa file trên máy chủ.
/// </summary>
public class FilePermissionCheckResult
{
    [JsonPropertyName("heDieuHanh")]
    public string HeDieuHanh { get; set; } = string.Empty;

    [JsonPropertyName("nguoiDungTienTrinh")]
    public string NguoiDungTienTrinh { get; set; } = string.Empty;

    [JsonPropertyName("thuMucApp")]
    public string ThuMucApp { get; set; } = string.Empty;

    [JsonPropertyName("thuMucDataWwwroot")]
    public string ThuMucDataWwwroot { get; set; } = string.Empty;

    [JsonPropertyName("taoThuMucThanhCong")]
    public bool TaoThuMucThanhCong { get; set; }

    [JsonPropertyName("ghiFileThanhCong")]
    public bool GhiFileThanhCong { get; set; }

    [JsonPropertyName("docFileThanhCong")]
    public bool DocFileThanhCong { get; set; }

    [JsonPropertyName("xoaFileThanhCong")]
    public bool XoaFileThanhCong { get; set; }

    [JsonPropertyName("chiTietLoi")]
    public string? ChiTietLoi { get; set; }
}
