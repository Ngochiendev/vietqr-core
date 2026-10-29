using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VietQR.Api.Models;
using VietQR.Core;

namespace VietQR.Api.Controllers;

/// <summary>
/// API sinh chuỗi và render ảnh mã thanh toán VietQR chuẩn NAPAS247.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/vietqr-core")]
[Route("api/v1/qr")]
[Produces("application/json")]
public class QrCodesController : ControllerBase
{
    private readonly IBankCatalog bankCatalog;
    private readonly QrImageService qrService;
    private readonly ILogger<QrCodesController> logger;

    public QrCodesController(
        IBankCatalog bankCatalog,
        QrImageService qrService,
        ILogger<QrCodesController> logger)
    {
        this.bankCatalog = bankCatalog;
        this.qrService = qrService;
        this.logger = logger;
    }

    /// <summary>
    /// Tạo mã thanh toán VietQR và ảnh thẻ chuẩn NAPAS247.
    /// </summary>
    /// <remarks>
    /// Endpoint cốt lõi sinh chuỗi mã EMVCo chuẩn NAPAS247 và render ảnh thẻ thanh toán (Base64 PNG).
    /// 
    /// **Các tham số bắt buộc:**
    /// - `bankBin`: Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số, VD: `970422` - MBBank, `970436` - Vietcombank).
    /// - `accountNo`: Số tài khoản, số thẻ hoặc tài khoản định danh/ảo (tối đa 25 ký tự).
    /// - `amount`: Số tiền thanh toán (VNĐ), phải là số nguyên dương lớn hơn 0.
    /// - `description`: Nội dung thanh toán (tối đa 95 ký tự theo chuẩn EMVCo).
    /// 
    /// **Các tham số tùy chọn:**
    /// - `accountName`: Tên chủ tài khoản người thụ hưởng.
    /// - `template`: Mẫu thiết kế giao diện ảnh thẻ (chấp nhận: `compact`, `qr_only`, `print`). Mặc định là `compact`.
    /// </remarks>
    /// <param name="bankBin">Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số).</param>
    /// <param name="accountNo">Số tài khoản, số thẻ hoặc tài khoản định danh/ảo (tối đa 25 ký tự).</param>
    /// <param name="amount">Số tiền thanh toán (VNĐ), số nguyên dương.</param>
    /// <param name="description">Nội dung chuyển khoản (tối đa 95 ký tự).</param>
    /// <param name="accountName">Tên chủ tài khoản (tùy chọn, hiển thị khi dùng mẫu 'print').</param>
    /// <param name="template">Mẫu thiết kế ảnh thẻ: 'compact' (mặc định), 'qr_only' hoặc 'print'.</param>
    /// <param name="serviceType">Loại dịch vụ chuyển tiền: 'account' (mặc định - QRIBFTTA) hoặc 'card' (QRIBFTTC).</param>
    /// <param name="billNumber">Số hóa đơn / mã đơn hàng (tùy chọn, Tag 62.01 tối đa 25 ký tự).</param>
    /// <response code="200">Tạo mã VietQR và render ảnh thành công (trả về qrCode và chuỗi Base64 qrDataURL).</response>
    /// <response code="400">Tham số yêu cầu không hợp lệ (mã code 01, 02, 03, 04, 05).</response>
    /// <response code="500">Lỗi ngoại lệ trong quá trình xử lý trên máy chủ (mã code 99).</response>
    [HttpGet("generate")]
    [EnableRateLimiting("generate")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
    [ProducesResponseType(typeof(ApiResponse<VietQrGenerateResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateVietQrCard(
        [FromQuery] string? bankBin = null,
        [FromQuery] string? accountNo = null,
        [FromQuery] string? amount = null,
        [FromQuery] string? description = null,
        [FromQuery] string? accountName = null,
        [FromQuery] string? template = "compact",
        [FromQuery] string? serviceType = null,
        [FromQuery] string? billNumber = null)
    {
        try
        {
            var missingParams = new List<string>();
            if (string.IsNullOrWhiteSpace(bankBin)) missingParams.Add("bankBin");
            if (string.IsNullOrWhiteSpace(accountNo)) missingParams.Add("accountNo");
            if (string.IsNullOrWhiteSpace(amount)) missingParams.Add("amount");
            if (string.IsNullOrWhiteSpace(description)) missingParams.Add("description");

            if (missingParams.Count > 0)
            {
                return BadRequest(new
                {
                    code = "01",
                    desc = $"Thiếu tham số bắt buộc: {string.Join(", ", missingParams)}. Yêu cầu truyền đủ cả 4 tham số: bankBin, accountNo, amount, description.",
                    data = (object?)null
                });
            }

            var cleanTemplate = string.IsNullOrWhiteSpace(template) ? "compact" : template.Trim().ToLowerInvariant();
            if (cleanTemplate != "compact" && cleanTemplate != "qr_only" && cleanTemplate != "print")
            {
                return BadRequest(new
                {
                    code = "05",
                    desc = $"Template '{template}' không hợp lệ. Chỉ hỗ trợ 3 mẫu: 'compact' (thẻ mặc định), 'qr_only' (chỉ có QR và logo ở giữa) hoặc 'print' (mẫu in ấn đầy đủ thông tin thanh toán).",
                    data = (object?)null
                });
            }

            var trimmedAmount = amount!.Trim();
            if (!trimmedAmount.All(c => c >= '0' && c <= '9') || trimmedAmount.Length > 13 || !ulong.TryParse(trimmedAmount, out var val) || val == 0)
            {
                return BadRequest(new
                {
                    code = "02",
                    desc = "Invalid amount - Trường chỉ nhập số, tối đa 13 kí tự",
                    data = (object?)null
                });
            }
            var cleanAmount = val.ToString();

            var cleanBin = bankBin!.Trim();
            if (cleanBin.Length != 6 || !cleanBin.All(c => c >= '0' && c <= '9'))
            {
                return BadRequest(new
                {
                    code = "03",
                    desc = $"Mã ngân hàng 'bankBin' ('{bankBin}') không hợp lệ. 'bankBin' bắt buộc phải là đúng 6 chữ số (VD: 970422). Không hỗ trợ dạng viết tắt như 'MB'.",
                    data = (object?)null
                });
            }

            var cleanAccountNo = accountNo!.Trim();
            if (cleanAccountNo.Length > 25)
            {
                return BadRequest(new
                {
                    code = "03",
                    desc = $"Số tài khoản/thẻ 'accountNo' không hợp lệ. Độ dài tối đa 25 ký tự theo chuẩn NAPAS / Virtual Account (nhận được {cleanAccountNo.Length} ký tự).",
                    data = (object?)null
                });
            }

            var cleanDescription = description!.Trim();
            if (cleanDescription.Length > 95)
            {
                return BadRequest(new
                {
                    code = "03",
                    desc = $"Nội dung chuyển tiền 'description' không hợp lệ. Độ dài tối đa 95 ký tự theo chuẩn EMVCo Tag 62 (nhận được {cleanDescription.Length} ký tự). Khuyến nghị tối đa 25 ký tự theo quy chuẩn NAPAS.",
                    data = (object?)null
                });
            }

            var bankInfo = await bankCatalog.FindAsync(cleanBin);
            if (bankInfo == null)
            {
                return BadRequest(new
                {
                    code = "04",
                    desc = $"Không tìm thấy thông tin ngân hàng trong danh mục cho mã BIN: {cleanBin}.",
                    data = (object?)null
                });
            }

            var resolvedServiceType = VietQrServiceType.Account;
            if (!string.IsNullOrWhiteSpace(serviceType))
            {
                var st = serviceType.Trim().ToLowerInvariant();
                if (st is "card" or "qribfttc" or "1")
                {
                    resolvedServiceType = VietQrServiceType.Card;
                }
            }

            var qrBuilder = new VietQr()
                .SetBeneficiaryOrganization(cleanBin, cleanAccountNo, resolvedServiceType)
                .SetTransactionAmount(cleanAmount)
                .SetAdditionalData(purpose: cleanDescription, billNumber: billNumber);

            var finalQrContent = qrBuilder.Build();
            var resolvedBankName = bankInfo?.ShortName ?? cleanBin;
            var resolvedLogoUrl = bankInfo?.Logo;

            string dataUrl = await qrService.GenerateVietQrBase64Async(
                finalQrContent,
                resolvedBankName,
                cleanBin,
                resolvedLogoUrl,
                cleanTemplate,
                qrOnly: false,
                accountNo: cleanAccountNo,
                accountName: accountName?.Trim(),
                amount: cleanAmount,
                description: cleanDescription,
                bankFullName: bankInfo?.Name,
                serviceType: resolvedServiceType);

            return Ok(new
            {
                code = "00",
                desc = "success",
                data = new
                {
                    qrCode = finalQrContent,
                    qrDataURL = dataUrl
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi tạo mã VietQR.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                code = "99",
                desc = "Đã xảy ra lỗi trong quá trình xử lý tạo mã VietQR trên máy chủ.",
                data = (object?)null
            });
        }
    }
}
