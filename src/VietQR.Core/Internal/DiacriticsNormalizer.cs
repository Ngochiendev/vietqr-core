using System.Globalization;
using System.Text;

namespace VietQR.Core.Internal;

/// <summary>
/// Tiện ích nội bộ chuẩn hóa tiếng Việt không dấu chuẩn ngân hàng.
/// </summary>
internal static class DiacriticsNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        bool isPureAscii = true;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] > 127 || char.IsControl(text[i]))
            {
                isPureAscii = false;
                break;
            }
        }
        if (isPureAscii) return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            if (char.IsControl(c)) continue;
            if (c is 'đ') { sb.Append('d'); continue; }
            if (c is 'Đ') { sb.Append('D'); continue; }

            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
