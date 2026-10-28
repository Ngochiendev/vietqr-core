using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using QRCoder;
using SkiaSharp;

namespace VietQR.Core;

/// <summary>
/// Service tạo ảnh thẻ VietQR chuẩn nhận diện Napas247.
/// </summary>
public class QrImageService
{
    private readonly IQrAssetResolver? _assetResolver;
    private readonly string? customRootPath;
    private readonly ILogger<QrImageService>? logger;

    public QrImageService(
        IQrAssetResolver? assetResolver = null,
        ILogger<QrImageService>? logger = null,
        string? customRootPath = null)
    {
        this._assetResolver = assetResolver;
        this.logger = logger;
        this.customRootPath = customRootPath;
    }

    public QrImageService(string customRootPath)
        : this(null, null, customRootPath)
    {
    }

    private static readonly ConcurrentDictionary<string, byte[]> LogoCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Render ảnh thẻ VietQR chuẩn PNG và trả về mảng byte raw.
    /// </summary>
    /// <param name="emvQrContent">Chuỗi mã QR chuẩn EMVCo (bắt buộc).</param>
    /// <param name="bankName">Tên viết tắt ngân hàng (VD: MBBank, VCB).</param>
    /// <param name="bankBin">Mã BIN ngân hàng thụ hưởng (VD: 970422).</param>
    /// <param name="bankLogoUrl">URL logo ngân hàng trên VietQR CDN (tùy chọn).</param>
    /// <param name="template">Mẫu giao diện: "compact" (thẻ chuẩn), "qr_only" (chỉ QR) hoặc "print" (mẫu in mở rộng).</param>
    /// <param name="qrOnly">Nếu true, luôn xuất mẫu QR vuông 500x500px kèm logo ở tâm.</param>
    /// <param name="accountNo">Số tài khoản hoặc thẻ người thụ hưởng.</param>
    /// <param name="accountName">Tên chủ tài khoản (hiển thị trên mẫu print).</param>
    /// <param name="amount">Số tiền VNĐ thanh toán.</param>
    /// <param name="description">Nội dung chuyển khoản.</param>
    /// <param name="bankFullName">Tên đầy đủ của ngân hàng.</param>
    /// <returns>Mảng byte ảnh PNG chất lượng cao.</returns>
    public async Task<byte[]> RenderPngBytesAsync(
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
        if (qrOnly || string.Equals(template, "qr_only", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateQrOnlyBytes(emvQrContent);
        }

        var base64Url = await GenerateVietQrBase64Async(
            emvQrContent, bankName, bankBin, bankLogoUrl, template, qrOnly, accountNo, accountName, amount, description, bankFullName);
        var base64 = base64Url.StartsWith("data:image/png;base64,") ? base64Url["data:image/png;base64,".Length..] : base64Url;
        return Convert.FromBase64String(base64);
    }

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

        // Tự động map tên ngân hàng và logo nếu chỉ truyền bankBin hoặc bankName
        if (!string.IsNullOrEmpty(bankBin))
        {
            var bankInfo = EmbeddedBankCatalog.Instance.Find(bankBin);
            if (bankInfo != null)
            {
                bankName ??= bankInfo.ShortName;
                bankFullName ??= bankInfo.Name;
                bankLogoUrl ??= bankInfo.Logo;
            }
        }
        else if (!string.IsNullOrEmpty(bankName))
        {
            var bankInfo = EmbeddedBankCatalog.Instance.Find(bankName);
            if (bankInfo != null)
            {
                bankBin ??= bankInfo.Bin;
                bankFullName ??= bankInfo.Name;
                bankLogoUrl ??= bankInfo.Logo;
            }
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

        int canvasWidth = isPrint ? 600 : 540;
        int canvasHeight = isPrint ? 590 + (printLines.Count * 28) + 16 : 540;
        using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var highSampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

        // Header: logo VietQR
        using var vietQrBmp = LoadVietQrLogoBitmap();
        if (vietQrBmp != null)
        {
            var srcRect = GetContentBounds(vietQrBmp);

            int targetH = isPrint ? 48 : 46;
            int targetW = (int)(targetH * (srcRect.Width / srcRect.Height));
            int logoX = (canvasWidth - targetW) / 2;
            int logoY = isPrint ? 24 : 18;

            using var paint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(vietQrBmp, srcRect, new SKRect(logoX, logoY, logoX + targetW, logoY + targetH), highSampling, paint);
        }

        // Khung viền và mã QR
        int qrFrameSize = 374;
        int frameX = (canvasWidth - qrFrameSize) / 2;
        int frameY = isPrint ? 96 : 84;

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0, 59, 112),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.5f,
            IsAntialias = true
        };
        canvas.DrawRect(new SKRect(frameX, frameY, frameX + qrFrameSize, frameY + qrFrameSize), borderPaint);

        // Chuẩn vietqr.io: Khoảng cách giữa viền xanh và mã QR chỉ ~9px (loại bỏ quiet zone 4 modules mặc định của QRCoder)
        int padding = 9;
        int innerQrSize = qrFrameSize - (padding * 2);
        int innerQrX = frameX + padding;
        int innerQrY = frameY + padding;

        var matrix = qrData.ModuleMatrix;
        int totalModules = matrix.Count;

        // Bỏ qua lề trắng (quiet zone) 4 module mặc định của QRCoder để QR sát với viền
        int quietZone = 0;
        while (quietZone < totalModules && !HasAnyTrueModule(matrix[quietZone]))
        {
            quietZone++;
        }

        int activeModuleCount = totalModules - (quietZone * 2);
        if (activeModuleCount <= 0)
        {
            activeModuleCount = totalModules;
            quietZone = 0;
        }

        float moduleSize = (float)innerQrSize / activeModuleCount;

        using var qrModulePaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        for (int row = 0; row < activeModuleCount; row++)
        {
            var rowModules = matrix[row + quietZone];
            for (int col = 0; col < activeModuleCount; col++)
            {
                if (rowModules[col + quietZone])
                {
                    float l = innerQrX + (col * moduleSize);
                    float t = innerQrY + (row * moduleSize);
                    float r = innerQrX + ((col + 1) * moduleSize);
                    float b = innerQrY + ((row + 1) * moduleSize);
                    canvas.DrawRect(new SKRect(l, t, r, b), qrModulePaint);
                }
            }
        }

        // Icon trung tâm QR
        using var centerIconBmp = LoadCenterIconBitmap();
        if (centerIconBmp != null)
        {
            int boxSize = 58;
            int boxX = (canvasWidth - boxSize) / 2;
            int boxY = frameY + (qrFrameSize - boxSize) / 2;

            using var bgPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(new SKRect(boxX, boxY, boxX + boxSize, boxY + boxSize), 5, 5, bgPaint);

            int iconSize = 46;
            int iconX = (canvasWidth - iconSize) / 2;
            int iconY = frameY + (qrFrameSize - iconSize) / 2;

            using var iconPaint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(centerIconBmp, new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize), highSampling, iconPaint);
        }

        // Footer: Napas247 và ngân hàng
        int footerCenterY = isPrint ? 516 : 495;
        int targetVisualHeight = 35;

        using var napasBmp = LoadNapasLogoBitmap();
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

            if (bankSrcRect.Height * bankScale > 42)
            {
                bankScale = 42f / bankSrcRect.Height;
            }
            if (bankSrcRect.Width * bankScale > 220)
            {
                bankScale = 220f / bankSrcRect.Width;
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
        canvas.DrawLine(sepX, footerCenterY - 18, sepX, footerCenterY + 18, linePaint);

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

            float currentY = 566f;
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


        using var surface = SKSurface.Create(new SKImageInfo(size, size));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        int padding = Math.Max(10, (int)(size * 0.02)); // Margin 10px chuẩn vietqr.io
        int innerQrSize = size - (padding * 2);

        var matrix = qrData.ModuleMatrix;
        int totalModules = matrix.Count;

        int quietZone = 0;
        while (quietZone < totalModules && !HasAnyTrueModule(matrix[quietZone]))
        {
            quietZone++;
        }

        int activeModuleCount = totalModules - (quietZone * 2);
        if (activeModuleCount <= 0)
        {
            activeModuleCount = totalModules;
            quietZone = 0;
        }

        float moduleSize = (float)innerQrSize / activeModuleCount;

        using var qrModulePaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        for (int row = 0; row < activeModuleCount; row++)
        {
            var rowModules = matrix[row + quietZone];
            for (int col = 0; col < activeModuleCount; col++)
            {
                if (rowModules[col + quietZone])
                {
                    float l = padding + (col * moduleSize);
                    float t = padding + (row * moduleSize);
                    float r = padding + ((col + 1) * moduleSize);
                    float b = padding + ((row + 1) * moduleSize);
                    canvas.DrawRect(new SKRect(l, t, r, b), qrModulePaint);
                }
            }
        }

        using var centerIconBmp = LoadCenterIconBitmap();
        if (centerIconBmp != null)
        {
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
    /// Lấy ảnh logo ngân hàng từ in-memory cache, Embedded Resources hoặc file cục bộ tùy biến.
    /// Hoạt động 100% Zero-I/O &amp; Offline, không gọi network hay ghi đĩa trên server.
    /// </summary>
    private Task<SKBitmap?> GetBankLogoBitmapAsync(string? bankBin, string? bankLogoUrl, string? bankName = null)
    {
        var cleanBin = bankBin?.Trim();
        var cleanName = bankName?.Trim();
        var cacheKey = !string.IsNullOrEmpty(cleanBin) ? cleanBin : (!string.IsNullOrEmpty(cleanName) ? cleanName : (bankLogoUrl?.Trim() ?? ""));

        if (!string.IsNullOrEmpty(cacheKey) && LogoCache.TryGetValue(cacheKey, out var cachedBytes))
        {
            return Task.FromResult<SKBitmap?>(SKBitmap.Decode(cachedBytes));
        }

        // 1. Đọc từ Embedded Resources theo mã BIN (76 ngân hàng thành viên NAPAS)
        if (!string.IsNullOrEmpty(cleanBin))
        {
            var embBytes = GetEmbeddedResourceBytes($"VietQR.Core.Resources.logos.{cleanBin}.png");
            if (embBytes != null && embBytes.Length > 0)
            {
                LogoCache[cacheKey] = embBytes;
                return Task.FromResult<SKBitmap?>(SKBitmap.Decode(embBytes));
            }
        }

        // 2. Tra cứu mã BIN qua Catalog bộ nhớ từ tên/mã ngân hàng
        var bankInfo = EmbeddedBankCatalog.Instance.Find(cleanBin)
                    ?? EmbeddedBankCatalog.Instance.Find(cleanName);
        if (bankInfo != null && !string.IsNullOrEmpty(bankInfo.Bin))
        {
            var embBytes = GetEmbeddedResourceBytes($"VietQR.Core.Resources.logos.{bankInfo.Bin}.png");
            if (embBytes != null && embBytes.Length > 0)
            {
                LogoCache[cacheKey] = embBytes;
                return Task.FromResult<SKBitmap?>(SKBitmap.Decode(embBytes));
            }
        }

        // 3. Fallback đọc từ đường dẫn file cục bộ (nếu người dùng cấu hình custom path)
        if (!string.IsNullOrWhiteSpace(bankLogoUrl) && File.Exists(bankLogoUrl))
        {
            try
            {
                var bytes = File.ReadAllBytes(bankLogoUrl);
                LogoCache[cacheKey] = bytes;
                return Task.FromResult<SKBitmap?>(SKBitmap.Decode(bytes));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Không thể đọc file logo từ đường dẫn: {Path}", bankLogoUrl);
            }
        }

        if (!string.IsNullOrEmpty(customRootPath))
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(cleanBin))
            {
                candidates.Add(Path.Combine(customRootPath, $"{cleanBin}.png"));
                candidates.Add(Path.Combine(customRootPath, "logos", $"{cleanBin}.png"));
            }
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(path);
                        LogoCache[cacheKey] = bytes;
                        return Task.FromResult<SKBitmap?>(SKBitmap.Decode(bytes));
                    }
                    catch { }
                }
            }
        }

        return Task.FromResult<SKBitmap?>(null);
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

    private static byte[]? GetEmbeddedResourceBytes(string resourceName)
    {
        try
        {
            var asm = typeof(QrImageService).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static SKBitmap? LoadEmbeddedBitmap(string resourceName)
    {
        var bytes = GetEmbeddedResourceBytes(resourceName);
        return bytes != null && bytes.Length > 0 ? SKBitmap.Decode(bytes) : null;
    }

    private SKBitmap? LoadVietQrLogoBitmap()
    {
        var emb = LoadEmbeddedBitmap("VietQR.Core.Resources.vietqr.png");
        if (emb != null) return emb;
        var path = GetDataFilePath("vietqr.png");
        return File.Exists(path) ? SKBitmap.Decode(path) : null;
    }

    private SKBitmap? LoadCenterIconBitmap()
    {
        var emb = LoadEmbeddedBitmap("VietQR.Core.Resources.vietqr_icon.png");
        if (emb != null) return emb;
        var path = GetDataFilePath("vietqr_icon.png");
        return File.Exists(path) ? SKBitmap.Decode(path) : null;
    }

    private SKBitmap? LoadNapasLogoBitmap()
    {
        var emb = LoadEmbeddedBitmap("VietQR.Core.Resources.napas247.png");
        if (emb != null) return emb;
        var path = GetDataFilePath("napas247.png");
        return File.Exists(path) ? SKBitmap.Decode(path) : null;
    }

    private string GetDataFilePath(string fileName)
    {
        if (_assetResolver != null)
        {
            var resolved = _assetResolver.Resolve(fileName);
            if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved)) return resolved;
        }

        if (!string.IsNullOrEmpty(customRootPath))
        {
            var p = Path.Combine(customRootPath, fileName);
            if (File.Exists(p)) return p;
            var pData = Path.Combine(customRootPath, "data", fileName);
            if (File.Exists(pData)) return pData;
        }

        return Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
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

    private static bool HasAnyTrueModule(System.Collections.BitArray row)
    {
        for (int i = 0; i < row.Length; i++)
        {
            if (row[i]) return true;
        }
        return false;
    }
}
