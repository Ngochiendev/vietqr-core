using System.Text;
using QRCoder;

namespace VietQR.Core;

/// <summary>
/// Trình vẽ mã VietQR dạng Vector SVG thuần C# (Zero native dependency).
/// Cho phép xuất ảnh vector sắc nét vô hạn cho in ấn standee, decal khổ lớn và Web responsive.
/// Hoạt động 100% độc lập, không phụ thuộc SkiaSharp C++ runtime.
/// </summary>
public static class VietQrSvgRenderer
{
    /// <summary>
    /// Render chuỗi mã VietQR thành chuỗi XML SVG hoàn chỉnh.
    /// </summary>
    /// <param name="emvQrContent">Chuỗi mã QR chuẩn EMVCo.</param>
    /// <param name="size">Kích thước canvas viewBox (pixel, mặc định 500).</param>
    /// <param name="darkColor">Mã màu hex của các điểm ảnh QR (mặc định "#000000").</param>
    /// <param name="lightColor">Mã màu nền (mặc định "#FFFFFF").</param>
    /// <param name="centerLogoBase64">Chuỗi Base64 ảnh logo nhúng vào tâm (tùy chọn).</param>
    /// <param name="centerLogoRatio">Tỷ lệ logo ở tâm so với mã QR (mặc định 0.18f ~ 18%).</param>
    /// <returns>Chuỗi XML SVG hoàn chỉnh có thể nhúng trực tiếp vào HTML hoặc lưu file .svg.</returns>
    public static string Render(
        string emvQrContent,
        int size = 500,
        string darkColor = "#000000",
        string lightColor = "#FFFFFF",
        string? centerLogoBase64 = null,
        float centerLogoRatio = 0.18f)
    {
        if (string.IsNullOrWhiteSpace(emvQrContent))
        {
            throw new ArgumentException("Chuỗi nội dung mã QR không được để trống.", nameof(emvQrContent));
        }

        if (size <= 0) size = 500;
        if (string.IsNullOrWhiteSpace(darkColor)) darkColor = "#000000";
        if (string.IsNullOrWhiteSpace(lightColor)) lightColor = "#FFFFFF";

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(emvQrContent, QRCodeGenerator.ECCLevel.Q);
        var matrix = qrData.ModuleMatrix;
        int totalModules = matrix.Count;

        // Bỏ qua quiet zone (lề trắng) 4 module mặc định để mã sát viền chuẩn VietQR
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

        int padding = Math.Max(10, (int)(size * 0.025f));
        float innerSize = size - (padding * 2);
        float moduleSize = innerSize / activeModuleCount;

        // Tính vùng tâm nếu có logo trung tâm
        int centerBoxStart = -1, centerBoxEnd = -1;
        float logoBoxSize = 0, logoBoxX = 0, logoBoxY = 0;
        bool hasCenterLogo = !string.IsNullOrWhiteSpace(centerLogoBase64);

        if (hasCenterLogo)
        {
            centerLogoRatio = Math.Clamp(centerLogoRatio, 0.10f, 0.28f);
            logoBoxSize = innerSize * centerLogoRatio;
            logoBoxX = padding + (innerSize - logoBoxSize) / 2f;
            logoBoxY = padding + (innerSize - logoBoxSize) / 2f;

            float relBoxStartX = (logoBoxX - padding) / moduleSize;
            float relBoxEndX = (logoBoxX + logoBoxSize - padding) / moduleSize;
            centerBoxStart = (int)Math.Floor(relBoxStartX);
            centerBoxEnd = (int)Math.Ceiling(relBoxEndX);
        }

        var sb = new StringBuilder(16384);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 {size} {size}\" width=\"{size}\" height=\"{size}\">");

        // Vẽ nền
        sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"{lightColor}\" />");

        // Gom các module thành các lệnh Path d="..." để tối ưu dung lượng SVG
        var pathBuilder = new StringBuilder();
        for (int row = 0; row < activeModuleCount; row++)
        {
            var rowModules = matrix[row + quietZone];
            for (int col = 0; col < activeModuleCount; col++)
            {
                if (rowModules[col + quietZone])
                {
                    // Nếu nằm trong ô logo trung tâm thì bỏ qua
                    if (hasCenterLogo && row >= centerBoxStart && row <= centerBoxEnd && col >= centerBoxStart && col <= centerBoxEnd)
                    {
                        continue;
                    }

                    float x = padding + (col * moduleSize);
                    float y = padding + (row * moduleSize);
                    pathBuilder.Append($"M{x:F2},{y:F2}h{moduleSize:F2}v{moduleSize:F2}h-{moduleSize:F2}z ");
                }
            }
        }

        if (pathBuilder.Length > 0)
        {
            sb.AppendLine($"  <path fill=\"{darkColor}\" d=\"{pathBuilder.ToString().TrimEnd()}\" />");
        }

        // Vẽ logo trung tâm (nếu có)
        if (hasCenterLogo)
        {
            float bgPadding = 4f;
            float bgX = logoBoxX - bgPadding;
            float bgY = logoBoxY - bgPadding;
            float bgSize = logoBoxSize + (bgPadding * 2);

            // Nền trắng bo góc cho logo
            sb.AppendLine($"  <rect x=\"{bgX:F2}\" y=\"{bgY:F2}\" width=\"{bgSize:F2}\" height=\"{bgSize:F2}\" rx=\"6\" ry=\"6\" fill=\"{lightColor}\" />");

            // Nhúng ảnh Base64
            var cleanHref = centerLogoBase64!.Trim();
            if (!cleanHref.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                cleanHref = $"data:image/png;base64,{cleanHref}";
            }

            sb.AppendLine($"  <image href=\"{cleanHref}\" x=\"{logoBoxX:F2}\" y=\"{logoBoxY:F2}\" width=\"{logoBoxSize:F2}\" height=\"{logoBoxSize:F2}\" preserveAspectRatio=\"xMidYMid meet\" />");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
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

