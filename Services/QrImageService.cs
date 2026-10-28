using QRCoder;
using SkiaSharp;
using System.Collections.Concurrent;

namespace vietqr_core.Services;

/// <summary>
/// Service tạo ảnh thẻ VietQR chuẩn nhận diện Napas247.
/// </summary>
public class QrImageService(
    IWebHostEnvironment? environment = null,
    IHttpClientFactory? httpClientFactory = null,
    ILogger<QrImageService>? logger = null)
{
    private static readonly ConcurrentDictionary<string, byte[]> LogoCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tạo ảnh thẻ VietQR chuẩn PNG và trả về chuỗi Data URL Base64.
    /// Hỗ trợ 3 mẫu template:
    /// - 'compact': Mẫu thẻ chuẩn 600x585 px (mặc định) có header VietQR, khung viền, mã QR và footer Napas247 + logo ngân hàng.
    /// - 'qr_only': Mẫu vuông 500x500 px chỉ có mã QR và icon VietQR ở chính giữa.
    /// - 'print': Mẫu in ấn mở rộng kèm thông tin số tiền, nội dung CK, tên chủ TK, số TK (in đậm) và tên ngân hàng.
    /// </summary>
    public async Task<string> GenerateVietQrBase64Async(
        string emvQrContent,
        string? bankName = null,
        string? bankBin = null,
        string? bankLogoUrl = null,
        string? template = "compact",
        bool qrOnly = false,
        string? accountNo = null,
        string? accountName = null,
        string? amount = null,
        string? description = null,
        string? bankFullName = null)
    {
        if (string.IsNullOrWhiteSpace(emvQrContent))
        {
            throw new ArgumentException("Chuỗi nội dung mã QR (emvQrContent) không được để trống.", nameof(emvQrContent));
        }

        if (qrOnly || string.Equals(template, "qr_only", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateQrOnlyBase64(emvQrContent);
        }

        bool isPrint = string.Equals(template, "print", StringComparison.OrdinalIgnoreCase);
        var printLines = new List<(string Text, bool IsBold, float FontSize)>();

        if (isPrint)
        {
            string formattedAmount = FormatAmountVnd(amount);
            printLines.Add(($"Số tiền: {formattedAmount}", false, 16f));

            if (!string.IsNullOrWhiteSpace(description))
            {
                printLines.Add(($"Nội dung CK: {description.Trim()}", false, 16f));
            }

            if (!string.IsNullOrWhiteSpace(accountName))
            {
                printLines.Add(($"Tên chủ TK: {accountName.Trim().ToUpperInvariant()}", false, 16f));
            }

            if (!string.IsNullOrWhiteSpace(accountNo))
            {
                printLines.Add(($"Số TK: {accountNo.Trim()}", true, 18f));
            }

            var displayBankName = !string.IsNullOrWhiteSpace(bankFullName) ? bankFullName.Trim() : (bankName ?? "");
            if (!string.IsNullOrWhiteSpace(displayBankName))
            {
                printLines.Add((displayBankName, false, 15f));
            }
        }

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(emvQrContent, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);

        byte[] qrBytes = qrCode.GetGraphic(20, false);
        using var qrBitmap = SKBitmap.Decode(qrBytes);

        int canvasWidth = 600;
        int canvasHeight = isPrint ? 585 + (printLines.Count * 28) + 20 : 585;
        using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var highSampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
        var qrSampling = new SKSamplingOptions(SKFilterMode.Nearest);

        // Header: logo VietQR
        var vietQrLogoPath = GetDataFilePath("vietqr.png");
        if (File.Exists(vietQrLogoPath))
        {
            using var vietQrBmp = SKBitmap.Decode(vietQrLogoPath);
            var srcRect = GetContentBounds(vietQrBmp);

            int targetH = 48;
            int targetW = (int)(targetH * (srcRect.Width / srcRect.Height));
            int logoX = (canvasWidth - targetW) / 2;
            int logoY = 16;

            using var paint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(vietQrBmp, srcRect, new SKRect(logoX, logoY, logoX + targetW, logoY + targetH), highSampling, paint);
        }

        // Khung viền và mã QR
        int qrFrameSize = 430;
        int frameX = (canvasWidth - qrFrameSize) / 2;
        int frameY = 82;

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0, 59, 112),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };
        canvas.DrawRect(new SKRect(frameX, frameY, frameX + qrFrameSize, frameY + qrFrameSize), borderPaint);

        int padding = 12;
        int innerQrSize = qrFrameSize - (padding * 2);
        int innerQrX = frameX + padding;
        int innerQrY = frameY + padding;

        using var qrPaint = new SKPaint();
        canvas.DrawBitmap(qrBitmap, new SKRect(innerQrX, innerQrY, innerQrX + innerQrSize, innerQrY + innerQrSize), qrSampling, qrPaint);

        // Icon trung tâm QR
        var centerIconPath = GetDataFilePath("vietqr_icon.png");
        if (File.Exists(centerIconPath))
        {
            using var centerIconBmp = SKBitmap.Decode(centerIconPath);

            int boxSize = 68;
            int boxX = (canvasWidth - boxSize) / 2;
            int boxY = frameY + (qrFrameSize - boxSize) / 2;

            using var bgPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(new SKRect(boxX, boxY, boxX + boxSize, boxY + boxSize), 6, 6, bgPaint);

            int iconSize = 54;
            int iconX = (canvasWidth - iconSize) / 2;
            int iconY = frameY + (qrFrameSize - iconSize) / 2;

            using var iconPaint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(centerIconBmp, new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize), highSampling, iconPaint);
        }

        // Footer: Napas247 và ngân hàng
        int footerCenterY = isPrint ? 550 : 552;
        int targetVisualHeight = 25; // Kích thước thanh thoát, nhỏ gọn chuẩn thẻ VietQR

        var napasLogoPath = GetDataFilePath("napas247.png");
        using var napasBmp = File.Exists(napasLogoPath) ? SKBitmap.Decode(napasLogoPath) : null;
        using var bankBmp = await GetBankLogoBitmapAsync(bankBin, bankLogoUrl, bankName);

        // Chuẩn bị kích thước và tâm đứng Napas
        int napasW = 0, napasH = 0;
        SKRect napasSrcRect = SKRect.Empty;
        float napasCenterY = 0;
        float scale = 1f;

        if (napasBmp != null)
        {
            (napasSrcRect, napasCenterY) = GetVisualBounds(napasBmp);
            scale = (float)targetVisualHeight / napasSrcRect.Height;
            napasW = (int)(napasSrcRect.Width * scale);
            napasH = (int)(napasSrcRect.Height * scale);
        }

        // Chuẩn bị kích thước và tâm đứng Bank logo
        int bankW = 0, bankH = 0;
        SKRect bankSrcRect = SKRect.Empty;
        float bankCenterY = 0;
        float bankScale = scale;
        SKRect bankTextBounds = SKRect.Empty;
        using var fallbackFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 24);
        using var fallbackPaint = new SKPaint { Color = new SKColor(0, 82, 155), IsAntialias = true };

        if (bankBmp != null)
        {
            (bankSrcRect, bankCenterY) = GetVisualBounds(bankBmp);

            // Tính scale phù hợp: Với template chuẩn lớn (>=800px) thì đồng bộ scale với napas,
            // với các logo kích thước độc lập (như MoMo 93x34, ZaloPay, Cake...) thì scale theo chiều cao mục tiêu.
            bankScale = (float)targetVisualHeight / Math.Max(1f, bankSrcRect.Height);
            if (bankBmp.Width >= 800 && bankBmp.Height >= 300)
            {
                bankScale = scale;
            }

            if (bankSrcRect.Height * bankScale > 32)
            {
                bankScale = 32f / bankSrcRect.Height;
            }
            if (bankSrcRect.Width * bankScale > 190)
            {
                bankScale = 190f / bankSrcRect.Width;
            }

            bankW = (int)(bankSrcRect.Width * bankScale);
            bankH = (int)(bankSrcRect.Height * bankScale);
        }
        else if (!string.IsNullOrWhiteSpace(bankName))
        {
            fallbackFont.MeasureText(bankName, out bankTextBounds, fallbackPaint);
            bankW = (int)bankTextBounds.Width;
            bankH = (int)bankTextBounds.Height;
        }

        // Tính toán căn giữa toàn bộ cụm: [Napas247] | [Ngân hàng]
        int gap = 14;
        int lineWidth = 2;
        int totalGroupWidth = napasW + gap + lineWidth + gap + bankW;
        int groupStartX = (canvasWidth - totalGroupWidth) / 2;

        int napasX = groupStartX;
        int sepX = napasX + napasW + gap + (lineWidth / 2);
        int bankX = sepX + (lineWidth / 2) + gap;

        // Vẽ đường phân cách dọc |
        using var linePaint = new SKPaint
        {
            Color = new SKColor(0, 59, 112),
            StrokeWidth = lineWidth,
            IsAntialias = true
        };
        canvas.DrawLine(sepX, footerCenterY - 17, sepX, footerCenterY + 17, linePaint);

        // Vẽ logo Napas247 căn chỉnh thẳng hàng ngang theo tâm chữ
        if (napasBmp != null)
        {
            float napasVisualOffset = napasCenterY - napasSrcRect.Top;
            int napasY = (int)Math.Round(footerCenterY - (napasVisualOffset * scale));
            using var napasPaint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(napasBmp, napasSrcRect, new SKRect(napasX, napasY, napasX + napasW, napasY + napasH), highSampling, napasPaint);
        }

        // Vẽ logo ngân hàng căn chỉnh thẳng hàng ngang theo tâm chữ
        if (bankBmp != null)
        {
            float bankVisualOffset = bankCenterY - bankSrcRect.Top;
            int bY = (int)Math.Round(footerCenterY - (bankVisualOffset * bankScale));
            using var bankPaint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(bankBmp, bankSrcRect, new SKRect(bankX, bY, bankX + bankW, bY + bankH), highSampling, bankPaint);
        }
        else if (!string.IsNullOrWhiteSpace(bankName))
        {
            float textY = footerCenterY - bankTextBounds.MidY;
            canvas.DrawText(bankName, bankX, textY, SKTextAlign.Left, fallbackFont, fallbackPaint);
        }

        // Vẽ các dòng thông tin bổ sung cho template 'print'
        if (isPrint && printLines.Count > 0)
        {
            using var textPaint = new SKPaint
            {
                Color = new SKColor(0, 59, 112),
                IsAntialias = true
            };

            using var tfRegular = GetPreferredTypeface(SKFontStyle.Normal);
            using var tfBold = GetPreferredTypeface(SKFontStyle.Bold);

            float currentY = 598f;
            foreach (var line in printLines)
            {
                using var font = new SKFont(line.IsBold ? tfBold : tfRegular, line.FontSize);
                canvas.DrawText(line.Text, canvasWidth / 2f, currentY, SKTextAlign.Center, font, textPaint);
                currentY += line.IsBold ? 29f : 26f;
            }
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        string base64 = Convert.ToBase64String(data.ToArray());

        return $"data:image/png;base64,{base64}";
    }

    /// <summary>
    /// Tạo ảnh chỉ chứa mã QR và icon VietQR ở chính giữa (chuẩn template 'qr_only').
    /// </summary>
    /// <param name="emvQrContent">Chuỗi nội dung mã QR chuẩn EMVCo.</param>
    /// <param name="size">Kích thước ảnh vuông xuất ra (mặc định 500x500 px).</param>
    /// <returns>Chuỗi Data URL Base64 (data:image/png;base64,...).</returns>
    public string GenerateQrOnlyBase64(string emvQrContent, int size = 500)
    {
        var bytes = GenerateQrOnlyBytes(emvQrContent, size);
        string base64 = Convert.ToBase64String(bytes);
        return $"data:image/png;base64,{base64}";
    }

    /// <summary>
    /// Tạo ảnh chỉ chứa mã QR và icon VietQR ở chính giữa (bất đồng bộ).
    /// </summary>
    public Task<string> GenerateQrOnlyBase64Async(string emvQrContent, int size = 500)
    {
        return Task.FromResult(GenerateQrOnlyBase64(emvQrContent, size));
    }

    /// <summary>
    /// Tạo mảng byte ảnh PNG chỉ chứa mã QR và icon VietQR ở chính giữa (chuẩn template 'qr_only').
    /// </summary>
    public byte[] GenerateQrOnlyBytes(string emvQrContent, int size = 500)
    {
        if (string.IsNullOrWhiteSpace(emvQrContent))
        {
            throw new ArgumentException("Chuỗi nội dung mã QR (emvQrContent) không được để trống.", nameof(emvQrContent));
        }

        if (size <= 0) size = 500;

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(emvQrContent, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);

        byte[] qrBytes = qrCode.GetGraphic(20, false);
        using var qrBitmap = SKBitmap.Decode(qrBytes);

        using var surface = SKSurface.Create(new SKImageInfo(size, size));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        int padding = Math.Max(16, (int)(size * 0.05));
        int innerQrSize = size - (padding * 2);

        using var qrPaint = new SKPaint();
        var qrSampling = new SKSamplingOptions(SKFilterMode.Nearest);
        canvas.DrawBitmap(qrBitmap, new SKRect(padding, padding, padding + innerQrSize, padding + innerQrSize), qrSampling, qrPaint);

        var centerIconPath = GetDataFilePath("vietqr_icon.png");
        if (File.Exists(centerIconPath))
        {
            using var centerIconBmp = SKBitmap.Decode(centerIconPath);

            int boxSize = (int)(innerQrSize * 0.18);
            int boxX = (size - boxSize) / 2;
            int boxY = (size - boxSize) / 2;
            int cornerRadius = Math.Max(4, (int)(boxSize * 0.12));

            using var bgPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(new SKRect(boxX, boxY, boxX + boxSize, boxY + boxSize), cornerRadius, cornerRadius, bgPaint);

            int iconSize = (int)(boxSize * 0.82);
            int iconX = (size - iconSize) / 2;
            int iconY = (size - iconSize) / 2;

            using var iconPaint = new SKPaint { IsAntialias = true };
            var highSampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
            canvas.DrawBitmap(centerIconBmp, new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize), highSampling, iconPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Lấy ảnh logo ngân hàng từ cache, file cục bộ hoặc tải từ CDN.
    /// </summary>
    private async Task<SKBitmap?> GetBankLogoBitmapAsync(string? bankBin, string? bankLogoUrl, string? bankName = null)
    {
        var logosDir = GetLogosDirectory();
        var cleanBin = bankBin?.Trim();
        var cleanName = bankName?.Trim();
        var cacheKey = !string.IsNullOrEmpty(cleanBin) ? cleanBin : (!string.IsNullOrEmpty(cleanName) ? cleanName : (bankLogoUrl?.Trim() ?? ""));

        if (!string.IsNullOrEmpty(cacheKey) && LogoCache.TryGetValue(cacheKey, out var cachedBytes))
        {
            return SKBitmap.Decode(cachedBytes);
        }

        // 1. Tìm file logo cục bộ theo mã BIN và tên ngân hàng
        var candidates = new List<string>();
        if (!string.IsNullOrEmpty(cleanBin))
        {
            candidates.Add(Path.Combine(logosDir, $"{cleanBin}.png"));
            candidates.Add(Path.Combine(logosDir, $"{cleanBin.ToUpperInvariant()}.png"));
            candidates.Add(Path.Combine(logosDir, $"{cleanBin.ToLowerInvariant()}.png"));
        }
        if (!string.IsNullOrEmpty(cleanName))
        {
            candidates.Add(Path.Combine(logosDir, $"{cleanName}.png"));
            candidates.Add(Path.Combine(logosDir, $"{cleanName.ToUpperInvariant()}.png"));
            candidates.Add(Path.Combine(logosDir, $"{cleanName.ToLowerInvariant()}.png"));
        }

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                var bytes = await File.ReadAllBytesAsync(path);
                LogoCache[cacheKey] = bytes;
                return SKBitmap.Decode(bytes);
            }
        }

        // 2. Kiểm tra đường dẫn file trực tiếp từ bankLogoUrl
        if (!string.IsNullOrWhiteSpace(bankLogoUrl) && File.Exists(bankLogoUrl))
        {
            var bytes = await File.ReadAllBytesAsync(bankLogoUrl);
            LogoCache[cacheKey] = bytes;
            return SKBitmap.Decode(bytes);
        }

        // 3. Tìm theo tên file từ URL trong thư mục logos
        string? urlToFetch = bankLogoUrl;
        if (!string.IsNullOrWhiteSpace(urlToFetch) && Uri.TryCreate(urlToFetch, UriKind.Absolute, out var uri))
        {
            var fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var localByName = Path.Combine(logosDir, fileName);
                if (File.Exists(localByName))
                {
                    var bytes = await File.ReadAllBytesAsync(localByName);
                    LogoCache[cacheKey] = bytes;
                    return SKBitmap.Decode(bytes);
                }
            }
        }

        // 4. Tra cứu URL logo và mã file trong banks.json theo BIN hoặc Code hoặc Name
        var banksJsonPath = GetDataFilePath("banks.json");
        if (File.Exists(banksJsonPath))
        {
            try
            {
                using var fs = new FileStream(banksJsonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var doc = await System.Text.Json.JsonDocument.ParseAsync(fs);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var b = item.TryGetProperty("bin", out var bp) ? bp.GetString() : null;
                        var c = item.TryGetProperty("code", out var cp) ? cp.GetString() : null;
                        var sn = item.TryGetProperty("shortName", out var snp) ? snp.GetString() : null;
                        var l = item.TryGetProperty("logo", out var lp) ? lp.GetString() : null;

                        bool matches = (!string.IsNullOrEmpty(cleanBin) && (string.Equals(b, cleanBin, StringComparison.OrdinalIgnoreCase) || string.Equals(c, cleanBin, StringComparison.OrdinalIgnoreCase)))
                            || (!string.IsNullOrEmpty(cleanName) && (string.Equals(sn, cleanName, StringComparison.OrdinalIgnoreCase) || string.Equals(c, cleanName, StringComparison.OrdinalIgnoreCase)));

                        if (matches)
                        {
                            if (!string.IsNullOrEmpty(b) && File.Exists(Path.Combine(logosDir, $"{b}.png")))
                            {
                                var bytes = await File.ReadAllBytesAsync(Path.Combine(logosDir, $"{b}.png"));
                                LogoCache[cacheKey] = bytes;
                                return SKBitmap.Decode(bytes);
                            }
                            if (!string.IsNullOrEmpty(c) && File.Exists(Path.Combine(logosDir, $"{c}.png")))
                            {
                                var bytes = await File.ReadAllBytesAsync(Path.Combine(logosDir, $"{c}.png"));
                                LogoCache[cacheKey] = bytes;
                                return SKBitmap.Decode(bytes);
                            }
                            if (!string.IsNullOrEmpty(l))
                            {
                                urlToFetch = l;
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Lỗi khi tra cứu logo từ banks.json cho BIN: {Bin}", cleanBin);
            }
        }

        // Tải từ CDN và lưu vào thư mục logos
        if (!string.IsNullOrWhiteSpace(urlToFetch) && Uri.TryCreate(urlToFetch, UriKind.Absolute, out var fetchUri))
        {
            try
            {
                Directory.CreateDirectory(logosDir);
                var client = httpClientFactory?.CreateClient("VietQrClient") ?? new HttpClient();
                var downloadedBytes = await client.GetByteArrayAsync(fetchUri);

                if (downloadedBytes.Length > 0)
                {
                    LogoCache[cacheKey] = downloadedBytes;

                    var fileName = Path.GetFileName(fetchUri.LocalPath);
                    if (!string.IsNullOrEmpty(cleanBin))
                    {
                        await File.WriteAllBytesAsync(Path.Combine(logosDir, $"{cleanBin}.png"), downloadedBytes);
                    }
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        await File.WriteAllBytesAsync(Path.Combine(logosDir, fileName), downloadedBytes);
                    }

                    return SKBitmap.Decode(downloadedBytes);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Không thể tải logo ngân hàng từ CDN: {Url}", urlToFetch);
            }
        }

        return null;
    }

    /// <summary>
    /// Tìm vùng chứa nội dung của ảnh và tâm đứng của phần chữ/nội dung chính.
    /// Tối ưu hóa đọc trực tiếp buffer bộ nhớ (loại bỏ P/Invoke GetPixel trong vòng lặp).
    /// </summary>
    private static (SKRect Bounds, float CenterY) GetVisualBounds(SKBitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int minX = width, maxX = 0;
        int minY = height, maxY = 0;
        bool found = false;
        double weightedY = 0;
        long totalWeight = 0;

        var span = bitmap.GetPixelSpan();
        int bytesPerPixel = bitmap.BytesPerPixel;
        int rowBytes = bitmap.RowBytes;

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * rowBytes;
            for (int x = 0; x < width; x++)
            {
                int offset = rowOffset + x * bytesPerPixel;
                byte r, g, b, a;

                if (bytesPerPixel == 4)
                {
                    if (bitmap.ColorType == SKColorType.Bgra8888)
                    {
                        b = span[offset];
                        g = span[offset + 1];
                        r = span[offset + 2];
                        a = span[offset + 3];
                    }
                    else
                    {
                        r = span[offset];
                        g = span[offset + 1];
                        b = span[offset + 2];
                        a = span[offset + 3];
                    }
                }
                else
                {
                    var pixel = bitmap.GetPixel(x, y);
                    r = pixel.Red; g = pixel.Green; b = pixel.Blue; a = pixel.Alpha;
                }

                bool isBackground = a <= 15 || (r >= 248 && g >= 248 && b >= 248);

                if (!isBackground)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    weightedY += y;
                    totalWeight++;
                    found = true;
                }
            }
        }

        if (!found) return (new SKRect(0, 0, width, height), height / 2f);
        float centerY = totalWeight > 0 ? (float)(weightedY / totalWeight) : (minY + maxY) / 2f;
        return (new SKRect(minX, minY, maxX + 1, maxY + 1), centerY);
    }

    /// <summary>
    /// Tìm vùng chứa nội dung của ảnh, bỏ qua viền trắng hoặc trong suốt.
    /// </summary>
    private static SKRect GetContentBounds(SKBitmap bitmap) => GetVisualBounds(bitmap).Bounds;

    private string GetDataFilePath(string fileName)
    {
        var webRoot = environment?.WebRootPath ?? (environment != null ? Path.Combine(environment.ContentRootPath, "wwwroot") : null);
        if (!string.IsNullOrEmpty(webRoot))
        {
            var p = Path.Combine(webRoot, "data", fileName);
            if (File.Exists(p)) return p;
        }

        // Tìm từ AppContext.BaseDirectory lên các thư mục cha
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "wwwroot", "data", fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        // Tìm từ Directory.GetCurrentDirectory() lên các thư mục cha
        var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (curDir != null)
        {
            var candidate = Path.Combine(curDir.FullName, "wwwroot", "data", fileName);
            if (File.Exists(candidate)) return candidate;
            curDir = curDir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "wwwroot", "data", fileName);
    }

    private string GetLogosDirectory()
    {
        var webRoot = environment?.WebRootPath ?? (environment != null ? Path.Combine(environment.ContentRootPath, "wwwroot") : null);
        if (!string.IsNullOrEmpty(webRoot))
        {
            var p = Path.Combine(webRoot, "data", "logos");
            if (Directory.Exists(p)) return p;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "wwwroot", "data", "logos");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (curDir != null)
        {
            var candidate = Path.Combine(curDir.FullName, "wwwroot", "data", "logos");
            if (Directory.Exists(candidate)) return candidate;
            curDir = curDir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "wwwroot", "data", "logos");
    }

    private static string FormatAmountVnd(string? rawAmount)
    {
        if (string.IsNullOrWhiteSpace(rawAmount)) return "0 VND";
        var cleaned = rawAmount.Replace(",", "").Replace(".", "").Trim();
        if (long.TryParse(cleaned, out var val))
        {
            var nfi = new System.Globalization.NumberFormatInfo
            {
                NumberGroupSeparator = ".",
                NumberDecimalDigits = 0
            };
            return $"{val.ToString("N0", nfi)} VND";
        }
        return $"{rawAmount} VND";
    }

    private static SKTypeface GetPreferredTypeface(SKFontStyle style)
    {
        var fontNames = new[] { "Arial", "Segoe UI", "SF Pro Text", "Roboto", "DejaVu Sans", "Liberation Sans" };
        foreach (var name in fontNames)
        {
            var tf = SKTypeface.FromFamilyName(name, style);
            if (tf != null && tf.FamilyName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return tf;
            }
        }
        return SKTypeface.FromFamilyName(null, style) ?? SKTypeface.Default;
    }
}
