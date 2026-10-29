namespace VietQR.Core;

/// <summary>
/// Định dạng ảnh xuất ra của mã QR thanh toán VietQR.
/// </summary>
public enum QrImageFormat
{
    /// <summary>
    /// Định dạng ảnh PNG tiêu chuẩn (Mặc định). Tương thích 100% với mọi nền tảng, thiết bị và trình duyệt.
    /// </summary>
    Png = 0,

    /// <summary>
    /// Định dạng ảnh WebP hiện đại. Tối ưu nén giúp giảm 30-40% dung lượng ảnh so với PNG, tiết kiệm băng thông mạng.
    /// </summary>
    Webp = 1,

    /// <summary>
    /// Định dạng ảnh Vector SVG (Scalable Vector Graphics). Độ phân giải vô hạn, sắc nét tuyệt đối trên màn hình Retina, Web Responsive và in ấn decal/standee khổ lớn.
    /// </summary>
    Svg = 2
}
