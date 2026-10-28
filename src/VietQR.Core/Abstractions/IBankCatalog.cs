namespace VietQR.Core;

/// <summary>Tra cứu ngân hàng theo BIN/code. Không lộ nguồn dữ liệu.</summary>
public interface IBankCatalog
{
    Task<BankInfo?> FindAsync(string binOrCode, CancellationToken ct = default);
    Task<IReadOnlyList<BankInfo>> GetAllAsync(CancellationToken ct = default);
}
