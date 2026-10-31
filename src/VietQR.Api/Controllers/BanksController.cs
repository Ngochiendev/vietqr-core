using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietQR.Core;

namespace VietQR.Api.Controllers;

/// <summary>
/// API quản lý danh mục ngân hàng theo chuẩn VietQR.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/vietqr-core")]
[Produces("application/json")]
public class BanksController : ControllerBase
{
    private readonly IBankCatalog bankCatalog;
    private readonly ILogger<BanksController> logger;

    public BanksController(
        IBankCatalog bankCatalog,
        ILogger<BanksController> logger)
    {
        this.bankCatalog = bankCatalog;
        this.logger = logger;
    }

    /// <summary>
    /// Lấy toàn bộ danh mục ngân hàng thành viên NAPAS.
    /// </summary>
    /// <remarks>
    /// Trả về 76 ngân hàng nhúng sẵn trong assembly, gồm: `bin`, `code`, `shortName`,
    /// `name` và `logo` (URL CDN `cdn.vietqr.io`). Response được cache 1 giờ.
    /// </remarks>
    /// <response code="200">Lấy danh mục ngân hàng thành công.</response>
    /// <example>
    /// ```json
    /// {
    ///   "code": "00",
    ///   "desc": "Thành công",
    ///   "data": [
    ///     { "bin": "970422", "code": "MB", "shortName": "MBBank", "name": "Ngan hang TMCP Quan Doi", "logo": "https://cdn.vietqr.io/img/MB.png" }
    ///   ]
    /// }
    /// ```
    /// </example>
    [HttpGet("banks")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanks(CancellationToken ct = default)
    {
        var all = await bankCatalog.GetAllAsync(ct);
        return Ok(new { code = "00", desc = "Thành công", data = all });
    }

    /// <summary>
    /// Tra cứu thông tin ngân hàng theo mã BIN hoặc mã code.
    /// </summary>
    /// <remarks>
    /// Nhập không phân biệt hoa-thường: `/banks/970422` hoặc `/banks/MB`.
    /// Dùng để kiểm tra BIN tồn tại trước khi gọi /generate (tránh mã lỗi 04).
    /// </remarks>
    /// <param name="binOrCode">Mã BIN gồm 6 chữ số (VD: 970422) hoặc mã code viết tắt (VD: MB, VCB).</param>
    /// <response code="200">Tìm thấy ngân hàng.</response>
    /// <response code="404">Không tìm thấy ngân hàng (mã lỗi 04).</response>
    /// <example>
    /// ```json
    /// {
    ///   "code": "00",
    ///   "desc": "Thành công",
    ///   "data": { "bin": "970422", "code": "MB", "shortName": "MBBank", "name": "Ngan hang TMCP Quan Doi", "logo": "https://cdn.vietqr.io/img/MB.png" }
    /// }
    /// ```
    /// </example>
    [HttpGet("banks/{binOrCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBankByBinOrCode(string binOrCode)
    {
        var bank = await bankCatalog.FindAsync(binOrCode);
        if (bank == null)
        {
            return NotFound(new { code = "04", desc = $"Không tìm thấy thông tin ngân hàng cho mã: {binOrCode}." });
        }
        return Ok(new { code = "00", desc = "Thành công", data = bank });
    }
}
