namespace VietQR.Core.Errors;

public static class VietQrErrors
{
    public static readonly Error MissingParam = new("qr.missing_param", "Thiếu tham số bắt buộc (bankBin, accountNo, amount, description).");
    public static readonly Error InvalidAmount = new("qr.invalid_amount", "Số tiền không hợp lệ (nhỏ hơn hoặc bằng 0).");
    public static readonly Error InvalidBin = new("qr.invalid_bin", "Mã BIN sai định dạng (bắt buộc đúng 6 chữ số).");
    public static readonly Error BankNotFound = new("qr.bank_not_found", "Không tìm thấy thông tin ngân hàng trong danh mục NAPAS.");
    public static readonly Error InvalidTemplate = new("qr.invalid_template", "Template không hợp lệ (chỉ chấp nhận 'compact', 'qr_only' hoặc 'print').");
    public static readonly Error SystemError = new("qr.system_error", "Lỗi xử lý hệ thống.");
}
