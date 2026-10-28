namespace VietQR.Core.Abstractions;

/// <summary>Giải đường dẫn asset — thay cho việc truyền object environment vào constructor.</summary>
public interface IQrAssetResolver
{
    string? Resolve(string fileName);
}
