using System.Globalization;
using System.Text;

namespace VietQR.Core;

/// <summary>
/// Bộ giải mã mã QR chuẩn VietQR (EMVCo / NAPAS247).
/// Phân tích cấu trúc TLV (Tag-Length-Value) đệ quy, xác thực checksum CRC16 và trích xuất dữ liệu thanh toán.
/// </summary>
public static class VietQrParser
{
    private const string VietQrGuid = "A000000727";

    /// <summary>
    /// Giải mã chuỗi mã QR theo chuẩn VietQR. Trả về đối tượng <see cref="Result{VietQrInfo}"/>.
    /// Phương thức an toàn, không ném exception khi chuỗi đầu vào bị lỗi hoặc độc hại.
    /// </summary>
    /// <param name="qrString">Chuỗi mã QR chuẩn EMVCo.</param>
    /// <param name="options">Cấu hình tùy chọn giải mã (strict CRC, max length, catalog).</param>
    /// <returns>Kết quả giải mã thành công chứa <see cref="VietQrInfo"/> hoặc lỗi cụ thể.</returns>
    public static Result<VietQrInfo> Parse(string? qrString, VietQrParseOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(qrString))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.InvalidPayload);
        }

        var opt = options ?? new VietQrParseOptions();

        if (qrString.Length > opt.MaxPayloadLength)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.InvalidPayload);
        }

        // 1. Kiểm tra CRC16 (Tag 63)
        bool isValidCrc = VietQr.VerifyCRC(qrString);
        if (opt.StrictCrc && !isValidCrc)
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.InvalidCrc);
        }

        // 2. Parse tầng Root TLV trên mảng byte UTF-8 (chuẩn ISO/IEC 7816 & EMVCo đo độ dài theo số byte octet)
        var utf8Bytes = Encoding.UTF8.GetBytes(qrString);
        var rootTags = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryParseTlvBlocks(utf8Bytes, rootTags))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.InvalidPayload);
        }

        // 3. Kiểm tra Tag 38 (Merchant Account Information của VietQR / NAPAS)
        if (!rootTags.TryGetValue("38", out var tag38Content))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.NotVietQr);
        }

        var tag38SubTags = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryParseTlvBlocks(Encoding.UTF8.GetBytes(tag38Content), tag38SubTags))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.InvalidPayload);
        }

        // Sub-tag 00: GUID phải bắt đầu hoặc chứa AID A000000727 của NAPAS
        if (!tag38SubTags.TryGetValue("00", out var guid) ||
            !guid.Equals(VietQrGuid, StringComparison.OrdinalIgnoreCase))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.NotVietQr);
        }

        // Sub-tag 01: Beneficiary Organization
        if (!tag38SubTags.TryGetValue("01", out var benOrgContent))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.NotVietQr);
        }

        var benOrgSubTags = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryParseTlvBlocks(Encoding.UTF8.GetBytes(benOrgContent), benOrgSubTags))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.InvalidPayload);
        }

        if (!benOrgSubTags.TryGetValue("00", out var bankBin) || string.IsNullOrWhiteSpace(bankBin))
        {
            return Result<VietQrInfo>.Fail(VietQrErrors.NotVietQr);
        }

        benOrgSubTags.TryGetValue("01", out var accountNo);
        accountNo ??= string.Empty;

        // Sub-tag 02: Service Code ("QRIBFTTA" -> Account, "QRIBFTTC" -> Card)
        var serviceType = VietQrServiceType.Account;
        if (tag38SubTags.TryGetValue("02", out var serviceCode))
        {
            if (string.Equals(serviceCode, "QRIBFTTC", StringComparison.OrdinalIgnoreCase))
            {
                serviceType = VietQrServiceType.Card;
            }
        }

        // 4. Khởi tạo đối tượng VietQrInfo
        var info = new VietQrInfo
        {
            BankBin = bankBin.Trim(),
            AccountNo = accountNo.Trim(),
            ServiceType = serviceType,
            IsValidCrc = isValidCrc,
            RawPayload = qrString,
            RawTags = rootTags
        };

        // Tra cứu ngân hàng từ Catalog
        var catalog = opt.BankCatalog ?? EmbeddedBankCatalog.Instance;
        info.BankInfo = catalog.Find(info.BankBin);

        // Tag 01: Point of Initiation Method ("11" = Static, "12" = Dynamic)
        if (rootTags.TryGetValue("01", out var pointMethod))
        {
            info.QrType = string.Equals(pointMethod.Trim(), "12", StringComparison.Ordinal)
                ? VietQrType.Dynamic
                : VietQrType.Static;
        }

        // Tag 53: Transaction Currency ("704" cho VND)
        if (rootTags.TryGetValue("53", out var currency))
        {
            info.Currency = currency.Trim();
        }

        // Tag 54: Transaction Amount
        if (rootTags.TryGetValue("54", out var amountStr))
        {
            if (decimal.TryParse(amountStr.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
            {
                info.Amount = amt;
            }
        }

        // Tag 58: Country Code ("VN")
        if (rootTags.TryGetValue("58", out var countryCode))
        {
            info.CountryCode = countryCode.Trim();
        }

        // Tag 59: Merchant Name
        if (rootTags.TryGetValue("59", out var merchantName))
        {
            info.MerchantName = merchantName.Trim();
        }

        // Tag 60: Merchant City
        if (rootTags.TryGetValue("60", out var merchantCity))
        {
            info.MerchantCity = merchantCity.Trim();
        }

        // Tag 62: Additional Data Field Template
        if (rootTags.TryGetValue("62", out var tag62Content))
        {
            var addTags = new Dictionary<string, string>(StringComparer.Ordinal);
            if (TryParseTlvBlocks(Encoding.UTF8.GetBytes(tag62Content), addTags))
            {
                var addOptions = new AdditionalDataOptions();

                if (addTags.TryGetValue("01", out var billNo))
                {
                    addOptions.BillNumber = billNo.Trim();
                    info.BillNumber = addOptions.BillNumber;
                }

                if (addTags.TryGetValue("05", out var refLabel))
                {
                    addOptions.ReferenceLabel = refLabel.Trim();
                    info.ReferenceLabel = addOptions.ReferenceLabel;
                }

                if (addTags.TryGetValue("08", out var purpose))
                {
                    addOptions.Purpose = purpose.Trim();
                    info.Description = addOptions.Purpose;
                }

                info.AdditionalData = addOptions;
            }
        }

        // Tag 63: CRC Checksum
        if (rootTags.TryGetValue("63", out var crcVal))
        {
            info.Crc = crcVal.Trim();
        }

        return Result<VietQrInfo>.Ok(info);
    }

    /// <summary>
    /// Thử giải mã chuỗi mã QR theo chuẩn VietQR. Trả về boolean an toàn cho nhánh if/else.
    /// </summary>
    public static bool TryParse(string? qrString, out VietQrInfo? info)
    {
        var result = Parse(qrString);
        if (result.IsSuccess && result.Value != null)
        {
            info = result.Value;
            return true;
        }

        info = null;
        return false;
    }

    /// <summary>
    /// Phân tích khối các cặp TLV (Tag-Length-Value) trong phạm vi byte octet một cách an toàn.
    /// Chuẩn ISO/IEC 7816 &amp; EMVCo đo độ dài Length theo số byte UTF-8.
    /// Không bao giờ ném ngoại lệ khi gặp dữ liệu bị cắt cụt hoặc sai cấu trúc.
    /// </summary>
    private static bool TryParseTlvBlocks(ReadOnlySpan<byte> span, Dictionary<string, string> result)
    {
        int index = 0;
        while (index < span.Length)
        {
            // Mỗi khối TLV cần tối thiểu 4 byte cho Tag (2 byte) và Length (2 byte)
            if (span.Length - index < 4)
            {
                return false;
            }

            byte t0 = span[index];
            byte t1 = span[index + 1];
            if (t0 < '0' || t0 > '9' || t1 < '0' || t1 > '9')
            {
                return false;
            }

            byte l0 = span[index + 2];
            byte l1 = span[index + 3];
            if (l0 < '0' || l0 > '9' || l1 < '0' || l1 > '9')
            {
                return false;
            }

            int length = (l0 - '0') * 10 + (l1 - '0');

            // Kiểm tra độ dài giá trị có vượt quá số byte còn lại của chuỗi không
            if (index + 4 + length > span.Length)
            {
                return false;
            }

            string tag = $"{(char)t0}{(char)t1}";
            string value = Encoding.UTF8.GetString(span.Slice(index + 4, length));

            result[tag] = value;
            index += 4 + length;
        }

        return true;
    }
}
