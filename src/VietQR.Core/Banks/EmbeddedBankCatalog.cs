using System.Text.Json;

namespace VietQR.Core;

/// <summary>
/// Danh mục ngân hàng được nhúng trực tiếp trong assembly VietQR.Core (chạy 100% offline, zero config).
/// </summary>
public sealed class EmbeddedBankCatalog : IBankCatalog
{
    private static readonly Lazy<EmbeddedBankCatalog> _lazy = new(() => new EmbeddedBankCatalog());
    public static EmbeddedBankCatalog Instance => _lazy.Value;

    private readonly Dictionary<string, BankInfo> _byBin = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BankInfo> _byCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<BankInfo> _allBanks = new();

    public EmbeddedBankCatalog()
    {
        LoadEmbeddedCatalog();
    }

    private void LoadEmbeddedCatalog()
    {
        try
        {
            var assembly = typeof(EmbeddedBankCatalog).Assembly;
            using var stream = assembly.GetManifestResourceStream("VietQR.Core.Resources.banks.json");
            if (stream == null) return;

            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return;

            foreach (var item in data.EnumerateArray())
            {
                var bin = item.TryGetProperty("bin", out var b) ? b.GetString() : null;
                var code = item.TryGetProperty("code", out var c) ? c.GetString() : null;
                var shortName = item.TryGetProperty("shortName", out var sn) ? sn.GetString() : null;
                var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                var logo = item.TryGetProperty("logo", out var l) ? l.GetString() : null;

                if (!string.IsNullOrEmpty(bin))
                {
                    var info = new BankInfo(bin, code ?? "", shortName ?? code ?? bin, name ?? "", logo ?? "");
                    _allBanks.Add(info);
                    _byBin[bin] = info;
                    if (!string.IsNullOrEmpty(code)) _byCode[code] = info;
                    if (!string.IsNullOrEmpty(shortName)) _byCode[shortName] = info;
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi nếu resource chưa sẵn sàng
        }
    }

    public Task<BankInfo?> FindAsync(string binOrCode, CancellationToken ct = default)
    {
        return Task.FromResult(Find(binOrCode));
    }

    public BankInfo? Find(string? binOrCode)
    {
        if (string.IsNullOrWhiteSpace(binOrCode)) return null;
        var key = binOrCode.Trim();

        if (_byBin.TryGetValue(key, out var byBin)) return byBin;
        if (_byCode.TryGetValue(key, out var byCode)) return byCode;

        return null;
    }

    public IReadOnlyList<BankInfo> GetAll() => _allBanks.AsReadOnly();

    public Task<IReadOnlyList<BankInfo>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<BankInfo>>(_allBanks.AsReadOnly());
    }
}
