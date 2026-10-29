namespace VietQR.Core;

/// <summary>Tra cứu ngân hàng theo BIN/code.</summary>
public interface IBankCatalog
{
    /// <summary>
    /// Tra cứu thông tin ngân hàng đồng bộ theo mã BIN hoặc mã/tên viết tắt (VD: 970422, MB, VCB).
    /// </summary>
    BankInfo? Find(string? binOrCode);

    /// <summary>
    /// Tra cứu thông tin ngân hàng bất đồng bộ theo mã BIN hoặc mã/tên viết tắt.
    /// </summary>
    Task<BankInfo?> FindAsync(string binOrCode, CancellationToken ct = default);

    /// <summary>
    /// Lấy toàn bộ danh sách ngân hàng thành viên.
    /// </summary>
    Task<IReadOnlyList<BankInfo>> GetAllAsync(CancellationToken ct = default);
}
