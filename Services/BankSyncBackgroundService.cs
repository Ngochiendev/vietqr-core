using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace vietqr_core.Services;

/// <summary>
/// Service đồng bộ danh sách ngân hàng định kỳ từ VietQR.
/// </summary>
public class BankSyncBackgroundService(
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment environment,
    ILogger<BankSyncBackgroundService> logger) : BackgroundService
{
    private const string ApiUrl = "https://api.vietqr.io/v2/banks";
    private readonly TimeZoneInfo _vnTimeZone = ResolveVietnamTimeZone();

    public static DateTime? LastSyncTimeUtc { get; private set; }
    public static string LastSyncStatus { get; private set; } = "Chưa chạy";
    public static string? LastSyncError { get; private set; }
    public static int LastBankCount { get; private set; }
    public static int LastDownloadedLogosCount { get; private set; }
    public TimeZoneInfo VietnamTimeZone => _vnTimeZone;
    public int CachedBanksCount => BankCache.Count;
    public TimeSpan DelayUntilNext1Am => GetDelayUntilNext1Am();

    private static readonly ConcurrentDictionary<string, BankInfo> BankCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<BankInfo?> FindBankAsync(string? binOrCode)
    {
        if (string.IsNullOrWhiteSpace(binOrCode)) return null;

        var key = binOrCode.Trim();
        if (BankCache.TryGetValue(key, out var cached)) return cached;

        await ReloadBankCacheAsync();
        return BankCache.TryGetValue(key, out var result) ? result : null;
    }

    private async Task ReloadBankCacheAsync()
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath)) return;

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var doc = await JsonDocument.ParseAsync(fs);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    var bin = item.TryGetProperty("bin", out var b) ? b.GetString() ?? "" : "";
                    var code = item.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
                    var shortName = item.TryGetProperty("shortName", out var sn) ? sn.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(shortName) && item.TryGetProperty("short_name", out var sn2))
                        shortName = sn2.GetString() ?? "";
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var logo = item.TryGetProperty("logo", out var l) ? l.GetString() ?? "" : "";

                    var info = new BankInfo(bin, code, shortName, name, logo);

                    if (!string.IsNullOrEmpty(bin)) BankCache[bin] = info;
                    if (!string.IsNullOrEmpty(code)) BankCache[code] = info;
                    if (!string.IsNullOrEmpty(shortName)) BankCache[shortName] = info;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể đọc dữ liệu banks.json để nạp cache tìm kiếm ngân hàng.");
        }
    }

    public string GetFilePath()
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "data");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "banks.json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var filePath = GetFilePath();
        await ReloadBankCacheAsync();

        if (ShouldSyncOnStartup(filePath))
        {
            await SyncBankListAsync(filePath, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNext1Am();
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await SyncBankListAsync(filePath, stoppingToken);
        }
    }

    public async Task<bool> SyncBankListAsync(string? targetPath = null, CancellationToken cancellationToken = default)
    {
        var filePath = targetPath ?? GetFilePath();

        try
        {
            var client = httpClientFactory.CreateClient("VietQrClient");
            var response = await client.GetAsync(ApiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LastSyncStatus = $"Lỗi HTTP: {response.StatusCode}";
                logger.LogWarning("Không thể tải danh sách từ VietQR: {StatusCode}", response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetString() != "00")
            {
                LastSyncStatus = "API trả về mã lỗi";
                logger.LogWarning("VietQR API trả về mã lỗi: {Json}", json);
                return false;
            }

            var count = root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
                ? data.GetArrayLength()
                : 0;

            if (count == 0)
            {
                LastSyncStatus = "VietQR API trả về 0 ngân hàng";
                return false;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var formattedJson = JsonSerializer.Serialize(root, jsonOptions);

            await SyncBankLogosAsync(root, cancellationToken);

            if (File.Exists(filePath))
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                var currentContent = await reader.ReadToEndAsync(cancellationToken);

                if (currentContent.Trim() == formattedJson.Trim())
                {
                    UpdateStatus($"Thành công (Không có thay đổi - {count} ngân hàng)", count);
                    return true;
                }
            }

            var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(tempPath, formattedJson, new UTF8Encoding(false), cancellationToken);
            File.Move(tempPath, filePath, overwrite: true);

            await ReloadBankCacheAsync();
            UpdateStatus($"Thành công (Đã cập nhật - {count} ngân hàng)", count);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            LastSyncStatus = "Lỗi phân quyền ghi file trên IIS";
            LastSyncError = $"Lỗi phân quyền IIS (UnauthorizedAccessException): Không có quyền ghi vào thư mục '{Path.GetDirectoryName(filePath)}'. Chi tiết: {ex.Message}";
            logger.LogError(ex, "Lỗi quyền IIS: Vui lòng cấp quyền Modify/Write cho IIS_IUSRS trên thư mục 'wwwroot/data'.");
            return false;
        }
        catch (Exception ex)
        {
            LastSyncStatus = $"Lỗi: {ex.Message}";
            LastSyncError = ex.ToString();
            logger.LogError(ex, "Có lỗi xảy ra khi đồng bộ ngân hàng từ VietQR.");
            return false;
        }
    }

    private bool ShouldSyncOnStartup(string filePath)
    {
        if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0) return true;

        var fileInfo = new FileInfo(filePath);
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTimeZone);
        var fileModifiedVn = TimeZoneInfo.ConvertTimeFromUtc(fileInfo.LastWriteTimeUtc, _vnTimeZone);

        // Nếu file đã cũ hơn 24 giờ
        if ((DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalHours >= 24) return true;

        // Hoặc hiện tại đã qua 1h sáng nhưng file được cập nhật từ trước 1h sáng hôm nay (bỏ lỡ lịch do IIS ngủ)
        var today1AmVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 1, 0, 0);
        return nowVn >= today1AmVn && fileModifiedVn < today1AmVn;
    }

    private TimeSpan GetDelayUntilNext1Am()
    {
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTimeZone);
        var next1AmVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, 1, 0, 0);

        if (nowVn >= next1AmVn)
        {
            next1AmVn = next1AmVn.AddDays(1);
        }

        return next1AmVn - nowVn;
    }

    private static void UpdateStatus(string status, int count)
    {
        LastSyncTimeUtc = DateTime.UtcNow;
        LastSyncStatus = status;
        LastBankCount = count;
        LastSyncError = null;
    }

    private async Task SyncBankLogosAsync(JsonElement root, CancellationToken cancellationToken)
    {
        try
        {
            var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var logosDir = Path.Combine(webRoot, "data", "logos");
            Directory.CreateDirectory(logosDir);

            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return;

            var client = httpClientFactory.CreateClient("VietQrClient");
            var banksToDownload = new List<(string bin, string url)>();

            foreach (var item in data.EnumerateArray())
            {
                var bin = item.TryGetProperty("bin", out var b) ? b.GetString() : null;
                var logoUrl = item.TryGetProperty("logo", out var l) ? l.GetString() : null;

                if (!string.IsNullOrEmpty(bin) && !string.IsNullOrWhiteSpace(logoUrl))
                {
                    var targetFile = Path.Combine(logosDir, $"{bin}.png");
                    if (!File.Exists(targetFile) || new FileInfo(targetFile).Length < 200)
                    {
                        banksToDownload.Add((bin, logoUrl));
                    }
                }
            }

            LastDownloadedLogosCount = banksToDownload.Count;

            if (banksToDownload.Count == 0)
            {
                logger.LogInformation("Toàn bộ logo ngân hàng đã tồn tại đầy đủ ({Dir}).", logosDir);
                return;
            }

            logger.LogInformation("Phát hiện {Count} logo ngân hàng cần tải về. Bắt đầu tải song song...", banksToDownload.Count);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(banksToDownload, parallelOptions, async (bank, ct) =>
            {
                try
                {
                    var bytes = await client.GetByteArrayAsync(bank.url, ct);
                    if (bytes.Length > 200)
                    {
                        var targetFile = Path.Combine(logosDir, $"{bank.bin}.png");
                        await File.WriteAllBytesAsync(targetFile, bytes, ct);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Không thể tải logo cho BIN {Bin} từ {Url}: {Msg}", bank.bin, bank.url, ex.Message);
                }
            });

            logger.LogInformation("Đã hoàn tất đồng bộ {Count} logo ngân hàng vào {Dir}.", banksToDownload.Count, logosDir);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi trong quá trình đồng bộ logo ngân hàng.");
        }
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } // Windows
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }   // Linux / macOS
            catch { return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam", "Vietnam"); }
        }
    }
}

public record BankInfo(string Bin, string Code, string ShortName, string Name, string Logo);

