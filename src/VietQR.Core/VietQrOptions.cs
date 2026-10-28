namespace VietQR.Core;

public class VietQrOptions
{
    public const string SectionName = "VietQr";

    public string DefaultTemplate { get; set; } = "compact";

    /// <summary>
    /// Đường dẫn tùy biến lưu trữ dữ liệu (banks.json, logos...). Nếu rỗng sẽ dùng wwwroot/data mặc định.
    /// Hỗ trợ thiết lập khi chạy Docker/Kubernetes với read-only container hoặc mounted volume.
    /// </summary>
    public string? DataPath { get; set; }
}
