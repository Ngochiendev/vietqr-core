using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietQR.Api.Models;
using VietQR.Core;
using VietQR.Deeplink.Client;

namespace VietQR.Api.Controllers;

/// <summary>
/// Tạo link mở nhanh app ngân hàng thông qua dịch vụ cổng VietQR Deeplink Client SDK.
/// </summary>
/// <remarks>
/// API công khai này không chứa scheme hay package của bất kỳ ngân hàng nào; nó chỉ chuyển tiếp tới cổng riêng
/// (cấu hình <c>Deeplink:BaseUrl</c>) và trả về link ngắn hạn.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/v1/vietqr-core/deeplink")]
[Produces("application/json")]
public class DeeplinkController : ControllerBase
{
    private readonly IVietQrDeeplinkClient client;

    public DeeplinkController(IVietQrDeeplinkClient client)
    {
        this.client = client;
    }

    /// <summary>
    /// Lấy danh sách các ứng dụng ngân hàng và ví điện tử hỗ trợ mở app (kèm cờ supportsAutofill).
    /// </summary>
    /// <param name="ct">Token hủy.</param>
    /// <response code="200">Danh sách ứng dụng được hỗ trợ.</response>
    [HttpGet("apps")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeeplinkBankAppInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetSupportedApps(CancellationToken ct)
    {
        if (!client.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Error("07", "Tính năng mở app ngân hàng chưa được cấu hình."));
        }

        try
        {
            var apps = await client.GetSupportedAppsAsync(ct);
            return Ok(ApiResponse<IReadOnlyList<DeeplinkBankAppInfo>>.Success(apps));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Error("07", "Không kết nối được dịch vụ mở app ngân hàng."));
        }
    }

    /// <summary>
    /// Tạo link ngắn hạn để mở app ngân hàng và tự điền thông tin chuyển khoản.
    /// </summary>
    /// <param name="request">Mã app/ngân hàng và chuỗi EMVCo VietQR.</param>
    /// <param name="ct">Token hủy.</param>
    /// <response code="200">Trả về link ngắn hạn (hết hạn sau vài chục giây).</response>
    /// <response code="400">Chuỗi VietQR không hợp lệ (sai khung hoặc sai CRC).</response>
    /// <response code="404">Cổng deep link không hỗ trợ ứng dụng này.</response>
    /// <response code="503">Chưa cấu hình hoặc không kết nối được cổng deep link.</response>
    [HttpPost("intents")]
    [ProducesResponseType(typeof(ApiResponse<DeeplinkIntentResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateIntent([FromBody] DeeplinkIntentRequest request, CancellationToken ct)
    {
        if (!client.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Error("07", "Tính năng mở app ngân hàng chưa được cấu hình."));
        }

        if (string.IsNullOrWhiteSpace(request.AppCode) ||
            string.IsNullOrWhiteSpace(request.QrCode) ||
            !VietQr.VerifyCRC(request.QrCode))
        {
            return BadRequest(ApiResponse<object>.Error("02", "Chuỗi VietQR không hợp lệ."));
        }

        try
        {
            var intent = await client.CreateIntentAsync(request.AppCode.Trim(), request.QrCode.Trim(), ct);
            if (intent is null)
            {
                return NotFound(ApiResponse<object>.Error("04", "Không hỗ trợ mở ứng dụng này hoặc yêu cầu bị từ chối."));
            }

            var forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
            var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
            var currentHost = !string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost : (Request.Host.HasValue ? Request.Host.Value : null);
            var currentScheme = !string.IsNullOrWhiteSpace(forwardedProto) ? forwardedProto : Request.Scheme;

            if (!string.IsNullOrWhiteSpace(currentHost) &&
                Uri.TryCreate(intent.Url, UriKind.Absolute, out var intentUri))
            {
                intent.Url = $"{currentScheme}://{currentHost}{intentUri.PathAndQuery}";
            }

            Response.Headers.CacheControl = "no-store";
            return Ok(ApiResponse<DeeplinkIntentResult>.Success(intent));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Error("07", "Không kết nối được dịch vụ mở app ngân hàng."));
        }
    }
}

/// <summary>
/// Yêu cầu tạo link mở app ngân hàng.
/// </summary>
public sealed class DeeplinkIntentRequest
{
    /// <summary>
    /// Mã ứng dụng, tên viết tắt hoặc BIN ngân hàng (VD: MB, TPB, 970422).
    /// </summary>
    /// <example>MB</example>
    public string AppCode { get; set; } = string.Empty;

    /// <summary>
    /// Chuỗi EMVCo VietQR (trường <c>qrCode</c> của API generate).
    /// </summary>
    public string QrCode { get; set; } = string.Empty;
}
