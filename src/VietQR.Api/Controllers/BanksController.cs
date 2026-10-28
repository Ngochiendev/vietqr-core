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
[Route("api/v1/banks")]
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
    /// Lấy toàn bộ danh sách ngân hàng trong hệ thống.
    /// </summary>
    /// <remarks>
    /// Trả về danh mục 76 ngân hàng (bao gồm mã BIN, Code, ShortName, Tên đầy đủ, Logo nhúng) trực tiếp từ bộ nhớ RAM.
    /// </remarks>
    /// <response code="200">Lấy danh mục ngân hàng thành công.</response>
    [HttpGet("banks")]
    [HttpGet("")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanks(CancellationToken ct = default)
    {
        var all = await bankCatalog.GetAllAsync(ct);
        return Ok(new { code = "00", desc = "Thành công", data = all });
    }

    /// <summary>
    /// Tra cứu thông tin ngân hàng theo mã BIN hoặc mã Code.
    /// </summary>
    /// <param name="binOrCode">Mã BIN (VD: 970422) hoặc mã viết tắt (VD: MB, VCB, TCB).</param>
    /// <response code="200">Tìm thấy thông tin ngân hàng.</response>
    /// <response code="404">Không tìm thấy ngân hàng tương ứng.</response>
    [HttpGet("{binOrCode}")]
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
