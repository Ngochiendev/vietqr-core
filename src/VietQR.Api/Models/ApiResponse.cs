using System.Text.Json.Serialization;

namespace VietQR.Api.Models;

/// <summary>
/// Cấu trúc chuẩn phản hồi của API VietQR Core.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của thuộc tính data.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Mã trạng thái kết quả xử lý (00: Thành công, 01-05: Lỗi tham số, 99: Lỗi hệ thống).
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

    public static ApiResponse<T> Success(T data, string desc = "success") => new()
    {
        Code = "00",
        Desc = desc,
        Data = data
    };

    public static ApiResponse<T> Error(string code, string desc) => new()
    {
        Code = code,
        Desc = desc,
        Data = default
    };
}
