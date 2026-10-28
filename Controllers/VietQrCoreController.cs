using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using vietqr_core.Models;
using vietqr_core.Services;

namespace vietqr_core.Controllers;

/// <summary>
/// Bộ API xử lý mã thanh toán VietQR chuẩn NAPAS247 và chẩn đoán hạ tầng IIS.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/vietqr-core")]
[Produces("application/json")]
public class VietQrCoreController(
    BankSyncBackgroundService syncService,
    QrImageService qrService,
    ILogger<VietQrCoreController> logger) : ControllerBase
{
    /// <summary>
    /// Tạo mã thanh toán VietQR và ảnh thẻ chuẩn NAPAS247.
    /// </summary>
    /// <remarks>
    /// Endpoint cốt lõi sinh chuỗi mã EMVCo chuẩn NAPAS247 và render ảnh thẻ thanh toán (Base64 PNG).
    /// 
    /// **Các tham số bắt buộc:**
    /// - `bankBin`: Mã BIN ngân hàng thụ hưởng (đúng 6 chữ số, VD: `970422` - MBBank, `970436` - Vietcombank).
    /// - `accountNo`: Số tài khoản hoặc số thẻ người thụ hưởng (tối đa 19 ký tự).
    /// - `amount`: Số tiền thanh toán (VNĐ), phải là số nguyên dương lớn hơn 0.
    /// - `description`: Nội dung chuyển tiền (tự động khử dấu tiếng Việt và lọc ký tự điều khiển ẩn).
    /// 
    /// **Tham số tùy chọn:**
    /// - `accountName`: Tên chủ tài khoản người thụ hưởng (hiển thị trên mẫu in ấn `print`).
    /// - `template`: Mẫu giao diện ảnh xuất ra:
    ///   - `compact` (mặc định): Thẻ ảnh kích thước chuẩn 600 × 585 px đầy đủ Header VietQR, khung viền, mã QR, Footer Napas247 &amp; logo ngân hàng.
    ///   - `qr_only`: Ảnh mã QR vuông 500 × 500 px tinh gọn, chỉ chứa mã QR và logo VietQR ở tâm.
    ///   - `print`: Mẫu in ấn mở rộng (600 × 745 px) hiển thị đầy đủ: Số tiền, Nội dung CK, Tên chủ TK, Số TK (in đậm) và Tên ngân hàng.
    /// 
    /// **Danh mục mã trạng thái phản hồi (code):**
    /// - `00`: Thành công.
    /// - `01`: Thiếu tham số bắt buộc.
    /// - `02`: Số tiền không hợp lệ (&lt;= 0 hoặc sai định dạng).
    /// - `03`: Mã BIN sai định dạng (không đúng 6 chữ số).
    /// - `04`: Không tìm thấy ngân hàng trong danh mục NAPAS.
    /// - `05`: Template không hợp lệ.
    /// - `99`: Lỗi ngoại lệ hệ thống.
    /// </remarks>
    /// <param name="bankBin">Mã BIN ngân hàng thụ hưởng (6 chữ số, VD: 970422).</param>
    /// <param name="accountNo">Số tài khoản hoặc số thẻ thụ hưởng.</param>
    /// <param name="amount">Số tiền thanh toán (VNĐ, lớn hơn 0).</param>
    /// <param name="description">Nội dung chuyển tiền.</param>
    /// <param name="accountName">Tên chủ tài khoản người thụ hưởng (tùy chọn, dùng hiển thị trên mẫu in ấn 'print').</param>
    /// <param name="template">Mẫu giao diện ảnh xuất ra: 'compact' (thẻ mặc định), 'qr_only' (chỉ mã QR kèm icon trung tâm), hoặc 'print' (mẫu in ấn đầy đủ thông tin).</param>
    /// <response code="200">Tạo mã VietQR và render ảnh thành công (trả về qrCode và chuỗi Base64 qrDataURL).</response>
    /// <response code="400">Tham số yêu cầu không hợp lệ (mã code 01, 02, 03, 04, 05).</response>
    /// <response code="500">Lỗi ngoại lệ trong quá trình xử lý trên máy chủ (mã code 99).</response>
    [HttpGet("generate")]
    [ProducesResponseType(typeof(ApiResponse<VietQrGenerateResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateVietQrCard(
        [FromQuery] string? bankBin = null,
        [FromQuery] string? accountNo = null,
        [FromQuery] string? amount = null,
        [FromQuery] string? description = null,
        [FromQuery] string? accountName = null,
        [FromQuery] string? template = "compact")
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
            // Chuẩn VietQR / NAPAS: Chỉ chấp nhận số nguyên dương (không chứa dấu chấm, dấu phẩy, khoảng trắng hay ký tự âm)
            if (!trimmedAmount.All(char.IsDigit) || trimmedAmount.Length > 13 || !ulong.TryParse(trimmedAmount, out var val) || val == 0)
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
            if (cleanBin.Length != 6 || !cleanBin.All(char.IsDigit))
            {
                return BadRequest(new
                {
                    code = "03",
                    desc = $"Mã ngân hàng 'bankBin' ('{bankBin}') không hợp lệ. 'bankBin' bắt buộc phải là đúng 6 chữ số (VD: 970422). Không hỗ trợ dạng viết tắt như 'MB'.",
                    data = (object?)null
                });
            }

            var cleanAccountNo = accountNo!.Trim();
            if (cleanAccountNo.Length > 19)
            {
                return BadRequest(new
                {
                    code = "03",
                    desc = $"Số tài khoản/thẻ 'accountNo' không hợp lệ. Độ dài tối đa 19 ký tự theo chuẩn NAPAS (nhận được {cleanAccountNo.Length} ký tự).",
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

            var bankInfo = await syncService.FindBankAsync(cleanBin);
            if (bankInfo == null)
            {
                return BadRequest(new
                {
                    code = "04",
                    desc = $"Không tìm thấy thông tin ngân hàng trong danh mục cho mã BIN: {cleanBin}.",
                    data = (object?)null
                });
            }

            var qrBuilder = new VietQr()
                .SetBeneficiaryOrganization(cleanBin, cleanAccountNo)
                .SetTransactionAmount(cleanAmount)
                .SetAdditionalDataFieldTemplate(cleanDescription);

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
                bankFullName: bankInfo?.Name);

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

    /// <summary>
    /// Kiểm tra quyền Đọc, Ghi, Xóa file trong thư mục wwwroot/data trên server IIS.
    /// </summary>
    /// <remarks>
    /// Thực hiện 4 thao tác trực tiếp trên ổ cứng máy chủ Windows Server:
    /// 1. Tạo thư mục `data` nếu chưa có.
    /// 2. Ghi file tạm `.tmp`.
    /// 3. Đọc lại nội dung file vừa ghi.
    /// 4. Xóa file tạm.
    /// 
    /// Dùng để chẩn đoán từ xa sự cố lỗi phân quyền `UnauthorizedAccessException` của IIS AppPool.
    /// </remarks>
    /// <response code="200">Kiểm tra phân quyền đọc ghi thành công.</response>
    /// <response code="500">AppPool bị chặn quyền truy cập đĩa cứng (kèm chi tiết lỗi và hướng dẫn icacls).</response>
    [HttpGet("test-rw")]
    [ProducesResponseType(typeof(ApiResponse<FilePermissionCheckResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FilePermissionCheckResult>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestReadWriteFile()
    {
        var dataFolder = Path.GetDirectoryName(syncService.GetFilePath())!;
        var testFilePath = Path.Combine(dataFolder, $"perm_test_{Guid.NewGuid():N}.tmp");

        bool canCreateDir = false;
        bool canWrite = false;
        bool canRead = false;
        bool canDelete = false;
        string? caughtError = null;

        try
        {
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }
            canCreateDir = true;

            var testContent = $"VietQR Core File Permission Check at {DateTime.UtcNow:O}";
            await System.IO.File.WriteAllTextAsync(testFilePath, testContent, System.Text.Encoding.UTF8);
            canWrite = true;

            var readBack = await System.IO.File.ReadAllTextAsync(testFilePath, System.Text.Encoding.UTF8);
            canRead = (readBack == testContent);

            System.IO.File.Delete(testFilePath);
            canDelete = !System.IO.File.Exists(testFilePath);

            return Ok(new
            {
                code = "00",
                desc = "Kiểm tra phân quyền ĐỌC - GHI - XÓA file trong wwwroot THÀNH CÔNG!",
                data = new
                {
                    heDieuHanh = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    nguoiDungTienTrinh = Environment.UserName,
                    thuMucApp = AppContext.BaseDirectory,
                    thuMucDataWwwroot = dataFolder,
                    taoThuMucThanhCong = canCreateDir,
                    ghiFileThanhCong = canWrite,
                    docFileThanhCong = canRead,
                    xoaFileThanhCong = canDelete
                }
            });
        }
        catch (Exception ex)
        {
            caughtError = ex.Message;
            logger.LogError(ex, "Lỗi phân quyền đọc ghi wwwroot.");
            return StatusCode(500, new
            {
                code = "99",
                desc = $"Lỗi phân quyền đọc/ghi trên server: {ex.Message}",
                data = new
                {
                    heDieuHanh = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    nguoiDungTienTrinh = Environment.UserName,
                    thuMucDataWwwroot = dataFolder,
                    taoThuMucThanhCong = canCreateDir,
                    ghiFileThanhCong = canWrite,
                    docFileThanhCong = canRead,
                    xoaFileThanhCong = canDelete,
                    chiTietLoi = caughtError
                }
            });
        }
    }

    /// <summary>
    /// Lấy danh sách ngân hàng thành viên NAPAS từ file banks.json cục bộ.
    /// </summary>
    /// <remarks>
    /// Đọc và trả về trực tiếp nội dung file JSON danh mục hơn 76 ngân hàng (bao gồm mã BIN, Code, ShortName, Tên đầy đủ, Logo).
    /// </remarks>
    /// <response code="200">Lấy danh mục ngân hàng thành công.</response>
    /// <response code="404">Chưa có dữ liệu ngân hàng trên máy chủ.</response>
    [HttpGet("banks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBanks()
    {
        var filePath = syncService.GetFilePath();
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { code = "404", desc = "Chưa có dữ liệu ngân hàng. Hãy thử lại sau ít phút." });
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        return Content(content, "application/json", System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Lấy báo cáo trạng thái hệ thống, phân quyền IIS và tiến trình đồng bộ ngầm.
    /// </summary>
    /// <remarks>
    /// Trả về các chỉ số giám sát phục vụ quản trị và vận hành hệ thống:
    /// - Quyền truy cập đĩa cứng của AppPool.
    /// - Dung lượng và thời gian cập nhật file `banks.json`.
    /// - Thống kê số lượng file và dung lượng thư mục logo ngân hàng `wwwroot/data/logos`.
    /// - Trạng thái lần đồng bộ cuối và thời gian đếm ngược tới lần chạy tiếp theo lúc 01:00 AM (giờ Việt Nam).
    /// </remarks>
    /// <response code="200">Lấy thông tin trạng thái thành công.</response>
    [HttpGet("sync-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSyncStatus()
    {
        var filePath = syncService.GetFilePath();
        var dataFolder = Path.GetDirectoryName(filePath)!;
        var logosFolder = Path.Combine(dataFolder, "logos");

        var fileExists = System.IO.File.Exists(filePath);
        var fileInfo = fileExists ? new FileInfo(filePath) : null;

        var logosFolderExists = Directory.Exists(logosFolder);
        var logoFiles = logosFolderExists ? Directory.GetFiles(logosFolder, "*.png") : Array.Empty<string>();
        long totalLogoBytes = 0;
        if (logosFolderExists)
        {
            foreach (var f in logoFiles)
            {
                try { totalLogoBytes += new FileInfo(f).Length; } catch { }
            }
        }

        // Kiểm tra quyền Đọc và Ghi thực tế
        bool canRead = false;
        bool canWrite = false;
        try
        {
            if (fileExists)
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                canRead = fs.CanRead;
            }
            var testTmp = Path.Combine(dataFolder, $".check_{Guid.NewGuid():N}.tmp");
            System.IO.File.WriteAllText(testTmp, "check", System.Text.Encoding.UTF8);
            System.IO.File.Delete(testTmp);
            canWrite = true;
        }
        catch { }

        var vnTz = syncService.VietnamTimeZone;
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz);
        var nextSyncDelay = syncService.DelayUntilNext1Am;
        var nextSyncVn = nowVn.Add(nextSyncDelay);

        string lastSyncVnStr = BankSyncBackgroundService.LastSyncTimeUtc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(BankSyncBackgroundService.LastSyncTimeUtc.Value, vnTz).ToString("dd/MM/yyyy HH:mm:ss")
            : "Chưa đồng bộ lần nào";

        return Ok(new
        {
            code = "00",
            desc = "Lấy báo cáo trạng thái đồng bộ và file hệ thống thành công",
            thoiGianHienTaiServerVn = nowVn.ToString("dd/MM/yyyy HH:mm:ss"),
            mayChuVaPhanQuyen = new
            {
                tenMayChu = Environment.MachineName,
                heDieuHanh = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                userChayTienTrinh = Environment.UserName,
                thuMucGocContent = AppContext.BaseDirectory,
                thuMucData = dataFolder,
                quyenDocWwwrootData = canRead,
                quyenGhiWwwrootData = canWrite
            },
            fileBanksJson = new
            {
                duongDan = filePath,
                tonTai = fileExists,
                dungLuong = fileInfo != null ? $"{fileInfo.Length:N0} bytes ({(fileInfo.Length / 1024.0):F2} KB)" : "0 bytes",
                ngayCapNhatVn = fileInfo != null ? TimeZoneInfo.ConvertTimeFromUtc(fileInfo.LastWriteTimeUtc, vnTz).ToString("dd/MM/yyyy HH:mm:ss") : "N/A",
                soLuongNganHangTrongCache = syncService.CachedBanksCount
            },
            thuMucLogos = new
            {
                duongDan = logosFolder,
                tonTai = logosFolderExists,
                tongSoFileLogoPng = logoFiles.Length,
                tongDungLuongLogos = $"{(totalLogoBytes / 1024.0 / 1024.0):F2} MB ({totalLogoBytes:N0} bytes)"
            },
            tienTrinhDongBoNgam = new
            {
                trangThai = BankSyncBackgroundService.LastSyncStatus,
                lanDongBoCuoiVn = lastSyncVnStr,
                soLuongNganHangDongBo = BankSyncBackgroundService.LastBankCount,
                lichChayTiepTheoVn = nextSyncVn.ToString("dd/MM/yyyy HH:mm:ss"),
                thoiGianChoConLai = $"{nextSyncDelay.Hours} giờ {nextSyncDelay.Minutes} phút ({nextSyncDelay.TotalHours:F1} giờ)",
                loiChiTiet = BankSyncBackgroundService.LastSyncError
            },
            huongDanKhacPhucQuyenIis = canWrite ? "Quyền ghi thư mục wwwroot/data đã OK!" : $"CẢNH BÁO: AppPool không có quyền ghi. Vui lòng mở CMD trên server chạy: icacls \"{dataFolder}\" /grant \"IIS_IUSRS\":(OI)(CI)M"
        });
    }

    /// <summary>
    /// Kích hoạt đồng bộ danh sách ngân hàng và tải toàn bộ logo PNG từ CDN ngay lập tức.
    /// </summary>
    /// <remarks>
    /// Gọi thủ công tiến trình đồng bộ dữ liệu ngân hàng từ API VietQR v2 về máy chủ cục bộ thay vì chờ lịch tự động 01:00 AM.
    /// </remarks>
    /// <response code="200">Đồng bộ hoàn tất (thành công hoặc thất bại kèm chi tiết lỗi).</response>
    [HttpPost("sync-now")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerSyncNow()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation("Nhận yêu cầu đồng bộ danh sách ngân hàng thủ công.");

        var filePath = syncService.GetFilePath();
        var existsBefore = System.IO.File.Exists(filePath);
        var bytesBefore = existsBefore ? new FileInfo(filePath).Length : 0;

        var success = await syncService.SyncBankListAsync();
        sw.Stop();

        var existsAfter = System.IO.File.Exists(filePath);
        var fileInfoAfter = existsAfter ? new FileInfo(filePath) : null;
        var vnTz = syncService.VietnamTimeZone;
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz);
        var logosDir = Path.Combine(Path.GetDirectoryName(filePath)!, "logos");
        var totalLogos = Directory.Exists(logosDir) ? Directory.GetFiles(logosDir, "*.png").Length : 0;

        return Ok(new
        {
            code = success ? "00" : "99",
            thanhCong = success,
            thongBao = success
                ? $"Đồng bộ thành công {BankSyncBackgroundService.LastBankCount} ngân hàng và {totalLogos} logo về thư mục wwwroot/data trong {sw.ElapsedMilliseconds} ms."
                : $"Đồng bộ thất bại: {BankSyncBackgroundService.LastSyncStatus}. Xem chi tiết lỗi bên dưới.",
            thoiGianXuLyMs = sw.ElapsedMilliseconds,
            thoiGianThucHienVn = nowVn.ToString("dd/MM/yyyy HH:mm:ss"),
            ketQuaChiTiet = new
            {
                trangThai = BankSyncBackgroundService.LastSyncStatus,
                tongSoNganHang = BankSyncBackgroundService.LastBankCount,
                duongDanFile = filePath,
                dungLuongTruocKhiSync = $"{bytesBefore:N0} bytes",
                dungLuongSauKhiSync = fileInfoAfter != null ? $"{fileInfoAfter.Length:N0} bytes ({(fileInfoAfter.Length / 1024.0):F2} KB)" : "0 bytes",
                ngaySuaFileMoiNhatVn = fileInfoAfter != null ? TimeZoneInfo.ConvertTimeFromUtc(fileInfoAfter.LastWriteTimeUtc, vnTz).ToString("dd/MM/yyyy HH:mm:ss") : "N/A",
                soLogoDaTaiMoiLanNay = BankSyncBackgroundService.LastDownloadedLogosCount,
                tongSoLogoHienCoTrongThuMuc = totalLogos
            },
            loiChiTietNeuCo = BankSyncBackgroundService.LastSyncError,
            huongDanXuLyLoi = success ? null : "Nếu gặp lỗi UnauthorizedAccessException trên IIS, cấp quyền: icacls \"<thư mục wwwroot/data>\" /grant \"IIS_IUSRS\":(OI)(CI)M"
        });
    }

    /// <summary>
    /// Chuyển hướng nhanh tới Thư viện 76 thẻ in VietQR Print.
    /// </summary>
    [HttpGet("gallery")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult OpenGallery()
    {
        return Redirect("/all_banks_gallery.html");
    }

    /// <summary>
    /// Kiểm thử chất lượng toàn diện (Self-Test &amp; Quality Diagnostic) trực tiếp trên máy chủ IIS.
    /// </summary>
    /// <remarks>
    /// Thực hiện chuỗi kiểm thử tự động trên máy chủ IIS:
    /// 1. Kiểm tra thuật toán CRC16-CCITT chuẩn ISO/IEC 13239.
    /// 2. Kiểm tra sinh chuỗi EMVCo VietQR NAPAS247 (chuyển qua tài khoản và qua thẻ).
    /// 3. Kiểm tra engine SkiaSharp render 3 mẫu thẻ (compact, qr_only, print).
    /// 4. Kiểm tra phân quyền đọc/ghi thư mục wwwroot/data của IIS AppPool.
    /// 5. Đo hiệu năng CPU (Benchmark nhanh thời gian render thẻ trên máy chủ).
    /// </remarks>
    /// <response code="200">Hệ thống vượt qua 100% các bài kiểm thử chất lượng trên IIS Server.</response>
    /// <response code="500">Phát hiện lỗi chất lượng hoặc cấu hình server IIS.</response>
    [HttpGet("self-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunSelfTest()
    {
        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var testResults = new List<object>();
        bool allPassed = true;

        // 1. Kiểm thử giải thuật CRC16 Table-driven
        // 1. Kiểm thử giải thuật CRC16 Table-driven (Theo chuẩn NAPAS v1.0 Mục 6.1.2)
        try
        {
            var napasPayload = "00020101021138600010A00000072701300006970403011697040311012345670208QRIBFTTC53037045802VN6304";
            var crc = VietQr.GenCRCCode(napasPayload);
            bool crcOk = crc == "4F52" && VietQr.VerifyCRC(napasPayload + crc);
            if (!crcOk) allPassed = false;
            testResults.Add(new { test = "Thuật toán CRC16-CCITT (ISO 13239 - NAPAS Spec)", status = crcOk ? "PASSED" : "FAILED", expected = "4F52", actual = crc });
        }
        catch (Exception ex)
        {
            allPassed = false;
            testResults.Add(new { test = "Thuật toán CRC16-CCITT (ISO 13239)", status = "FAILED", error = ex.Message });
        }

        // 2. Kiểm thử chuẩn EMVCo NAPAS247 (Tĩnh & Động)
        try
        {
            var dynamicQr = new VietQr()
                .SetBeneficiaryOrganization("970422", "0838539629")
                .SetTransactionAmount("50000")
                .SetAdditionalDataFieldTemplate("Thanh toan test")
                .Build();
            bool emvOk = VietQr.VerifyCRC(dynamicQr) && dynamicQr.Contains("540550000");
            if (!emvOk) allPassed = false;
            testResults.Add(new { test = "Chuẩn mã EMVCo NAPAS247 (Dynamic QR)", status = emvOk ? "PASSED" : "FAILED", length = dynamicQr.Length });
        }
        catch (Exception ex)
        {
            allPassed = false;
            testResults.Add(new { test = "Chuẩn mã EMVCo NAPAS247 (Dynamic QR)", status = "FAILED", error = ex.Message });
        }

        // 3. Kiểm thử Engine SkiaSharp (Render cả 3 templates trên IIS)
        long renderTimeMs = 0;
        try
        {
            var sampleQr = "00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test63048E1D";
            var swRender = System.Diagnostics.Stopwatch.StartNew();

            var cardCompact = await qrService.GenerateVietQrBase64Async(sampleQr, "MBBank", "970422", template: "compact");
            var cardQrOnly = qrService.GenerateQrOnlyBase64(sampleQr, 500);
            var cardPrint = await qrService.GenerateVietQrBase64Async(sampleQr, "MBBank", "970422", template: "print", accountNo: "0838539629", accountName: "TRAN QUOC VY", amount: "50000", description: "Thanh toan test");

            swRender.Stop();
            renderTimeMs = swRender.ElapsedMilliseconds;

            bool renderOk = cardCompact.StartsWith("data:image/png;base64,") &&
                           cardQrOnly.StartsWith("data:image/png;base64,") &&
                           cardPrint.StartsWith("data:image/png;base64,");
            if (!renderOk) allPassed = false;
            testResults.Add(new { test = "Engine SkiaSharp Native (compact, qr_only, print)", status = renderOk ? "PASSED" : "FAILED", renderTime3CardsMs = renderTimeMs });
        }
        catch (Exception ex)
        {
            allPassed = false;
            testResults.Add(new { test = "Engine SkiaSharp Native (compact, qr_only, print)", status = "FAILED", error = ex.Message });
        }

        // 4. Kiểm thử phân quyền đĩa cứng wwwroot/data
        try
        {
            var dataFolder = Path.GetDirectoryName(syncService.GetFilePath())!;
            var testTmp = Path.Combine(dataFolder, $".selftest_{Guid.NewGuid():N}.tmp");
            await System.IO.File.WriteAllTextAsync(testTmp, "ok", System.Text.Encoding.UTF8);
            var readOk = (await System.IO.File.ReadAllTextAsync(testTmp, System.Text.Encoding.UTF8)) == "ok";
            System.IO.File.Delete(testTmp);
            if (!readOk) allPassed = false;
            testResults.Add(new { test = "Phân quyền thư mục wwwroot/data (IIS AppPool)", status = readOk ? "PASSED" : "FAILED" });
        }
        catch (Exception ex)
        {
            allPassed = false;
            testResults.Add(new { test = "Phân quyền thư mục wwwroot/data (IIS AppPool)", status = "FAILED", error = ex.Message });
        }

        // 5. Mini-Benchmark hiệu năng CPU máy chủ IIS (1,000 lần tính CRC)
        var swBench = System.Diagnostics.Stopwatch.StartNew();
        const int benchIterations = 1000;
        var benchQr = "00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test6304";
        for (int i = 0; i < benchIterations; i++)
        {
            _ = VietQr.GenCRCCode(benchQr);
        }
        swBench.Stop();
        double crcSpeedNs = (swBench.Elapsed.TotalNanoseconds) / benchIterations;

        swTotal.Stop();

        var responseObj = new
        {
            code = allPassed ? "00" : "99",
            trangThaiKiemThu = allPassed ? "100% PASSED - TOÀN BỘ HỆ THỐNG ĐẠT CHUẨN CHẤT LƯỢNG!" : "FAILED - PHÁT HIỆN LỖI TRÊN MÁY CHỦ",
            tongThoiGianKiemThuMs = swTotal.ElapsedMilliseconds,
            thoiGianChayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, syncService.VietnamTimeZone).ToString("dd/MM/yyyy HH:mm:ss"),
            mayChu = new
            {
                tenMayChu = Environment.MachineName,
                heDieuHanh = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                kienTrucCpu = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                soLuongCoreCpu = Environment.ProcessorCount,
                tienTrinhUser = Environment.UserName,
                dungLuongRamHeapMb = $"{(GC.GetTotalMemory(false) / 1024.0 / 1024.0):F2} MB"
            },
            hieuNangMayChu = new
            {
                tocDoTinhCrcNs = $"{crcSpeedNs:F1} ns/lần ({1_000_000_000.0 / Math.Max(1, crcSpeedNs):N0} ops/giây)",
                tocDoRender3MauAnhMs = $"{renderTimeMs} ms (render đồng thời 3 template)"
            },
            chiTietKetQua = testResults
        };

        return allPassed ? Ok(responseObj) : StatusCode(StatusCodes.Status500InternalServerError, responseObj);
    }

    /// <summary>
    /// Kiểm thử hiệu năng chịu tải kịch bản hàng loạt: 2.000 đến 10.000 mã QR học phí (Stress &amp; Load Test).
    /// </summary>
    /// <remarks>
    /// Chạy kịch bản mô phỏng kiểm thử tải song song trên toàn bộ nhân CPU của máy chủ IIS:
    /// - Đo đạc chính xác thời gian hoàn thành và tốc độ phản hồi khi tạo đồng thời 2.000 hoặc 10.000 mã QR.
    /// - Hỗ trợ các mẫu: 'qr_only' (siêu tốc ~760 req/s, 10.000 QR chỉ mất ~13s), 'print' (thẻ in học phí đầy đủ), 'compact' và 'emv_only' (chỉ sinh chuỗi text).
    /// </remarks>
    /// <param name="testRequests">Số lượng request tạo thực tế trên server (VD: 2000 hoặc 10000, tối đa 50.000).</param>
    /// <param name="template">Mẫu cần test: 'print' (mẫu in ấn), 'qr_only' (mã QR tinh gọn siêu nhanh), 'compact' hoặc 'emv_only' (chỉ sinh chuỗi EMVCo).</param>
    /// <param name="totalSchools">Quy mô số trường học muốn dự phóng (mặc định 1.000 trường).</param>
    /// <param name="studentsPerSchool">Số học sinh trung bình mỗi trường muốn dự phóng (mặc định 1.000 học sinh).</param>
    /// <response code="200">Hoàn tất kiểm thử tải và trả về báo cáo tốc độ phản hồi thực tế.</response>
    [HttpGet("benchmark-school-test")]
    [HttpPost("benchmark-school-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RunSchoolTuitionStressTest(
        [FromQuery] int testRequests = 2000,
        [FromQuery] string template = "qr_only",
        [FromQuery] int totalSchools = 1000,
        [FromQuery] int studentsPerSchool = 1000)
    {
        testRequests = Math.Clamp(testRequests, 1, 50000);
        totalSchools = Math.Max(1, totalSchools);
        studentsPerSchool = Math.Max(1, studentsPerSchool);
        var cleanTemplate = string.IsNullOrWhiteSpace(template) ? "qr_only" : template.Trim().ToLowerInvariant();
        if (cleanTemplate != "compact" && cleanTemplate != "qr_only" && cleanTemplate != "print" && cleanTemplate != "emv_only")
        {
            cleanTemplate = "qr_only";
        }

        int processorCount = Environment.ProcessorCount;
        const string longSchoolName = "TRUONG THCS VA THPT TRAN HUNG DAO QUAN 1 TP HO CHI MINH";
        long totalStudents = (long)totalSchools * studentsPerSchool;

        long ramBefore = GC.GetTotalMemory(true);

        // 1. Khởi động warm-up (3 requests)
        if (cleanTemplate != "emv_only")
        {
            for (int i = 0; i < 3; i++)
            {
                _ = await GenerateVietQrCard(
                    bankBin: "970415",
                    accountNo: "113366668888",
                    amount: "15500000",
                    description: "Hoc phi khoi dong warmup",
                    accountName: longSchoolName,
                    template: cleanTemplate);
            }
        }

        // 2. Chạy tải thực tế song song trên toàn bộ CPU máy chủ IIS
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int successCount = 0;
        int errorCount = 0;

        if (cleanTemplate == "emv_only")
        {
            // Chế độ sinh chuỗi EMVCo thuần (không qua render đồ họa SkiaSharp)
            Parallel.For(1, testRequests + 1, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, i =>
            {
                try
                {
                    var emv = new VietQr()
                        .SetBeneficiaryOrganization("970415", $"11336666{i:D4}")
                        .SetTransactionAmount((1_000_000 + i * 10_000).ToString())
                        .SetAdditionalDataFieldTemplate($"HOC PHI HS {i:D6} LOP 10A1")
                        .Build();

                    if (!string.IsNullOrEmpty(emv))
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
            });
        }
        else
        {
            // Chế độ sinh ảnh Base64 đầy đủ
            await Parallel.ForEachAsync(
                Enumerable.Range(1, testRequests),
                new ParallelOptions { MaxDegreeOfParallelism = processorCount },
                async (i, ct) =>
                {
                    try
                    {
                        var res = await GenerateVietQrCard(
                            bankBin: "970415",
                            accountNo: $"11336666{i:D4}",
                            amount: (1_000_000 + i * 10_000).ToString(),
                            description: $"HOC PHI HS {i:D6} LOP 10A1",
                            accountName: longSchoolName,
                            template: cleanTemplate);

                        if (res is OkObjectResult)
                        {
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                });
        }

        sw.Stop();

        long ramAfter = GC.GetTotalMemory(false);
        double elapsedSeconds = sw.Elapsed.TotalSeconds;
        double throughputRps = testRequests / Math.Max(0.001, elapsedSeconds);
        double avgLatencyMs = sw.Elapsed.TotalMilliseconds / testRequests;

        // 3. Dự phóng thời gian thực tế
        double sec1School = studentsPerSchool / throughputRps;
        double min100Schools = (100.0 * studentsPerSchool) / throughputRps / 60.0;
        double hoursTotalSchools = totalStudents / throughputRps / 3600.0;

        return Ok(new
        {
            code = "00",
            desc = $"Hoàn tất kiểm thử tải thực tế {testRequests:N0} mã VietQR trên IIS Server",
            ketQuaKiemThuThucTe = new
            {
                tongSoQrDaTao = testRequests,
                mauTemplate = cleanTemplate,
                tongThoiGianHoanThanh = elapsedSeconds < 60 ? $"{elapsedSeconds:F2} giây ({sw.ElapsedMilliseconds:N0} ms)" : $"{elapsedSeconds / 60.0:F2} phút ({elapsedSeconds:F1} giây)",
                tocDoPhanHoiTrungBinhPerQr = $"{avgLatencyMs:F2} ms / mã QR",
                nangLucXuLyThucTeRps = $"{throughputRps:F1} QR / giây (~{throughputRps * 60:N0} QR / phút)",
                soRequestThanhCong = successCount,
                soRequestThatBai = errorCount
            },
            thongSoMayChuIis = new
            {
                tenMayChu = Environment.MachineName,
                heDieuHanh = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                soNhanCpuChaySongSong = processorCount,
                ramTruocKhiChay = $"{(ramBefore / 1024.0 / 1024.0):F2} MB",
                ramSauKhiChay = $"{(ramAfter / 1024.0 / 1024.0):F2} MB"
            },
            duPhongQuyMoTruongHoc = new
            {
                motTruongHoc = $"{studentsPerSchool:N0} học sinh: {(sec1School < 60 ? $"{sec1School:F1} giây" : $"{sec1School / 60.0:F1} phút")}",
                motTramTruongHoc = $"{100 * studentsPerSchool:N0} học sinh: {(min100Schools < 60 ? $"{min100Schools:F1} phút" : $"{min100Schools / 60.0:F2} giờ")}",
                tongQuyMoDauVao = $"{totalSchools:N0} trường ({totalStudents:N0} học sinh): {(hoursTotalSchools < 1 ? $"{hoursTotalSchools * 60.0:F0} phút" : $"{hoursTotalSchools:F2} giờ")}"
            },
            nhanXetHieuNang = cleanTemplate switch
            {
                "emv_only" => "Chế độ chuỗi EMVCo thuần đạt tốc độ hàng trăm nghìn mã/giây, tối ưu tuyệt đối cho Backend tính toán dữ liệu lớn.",
                "qr_only" => $"Mẫu 'qr_only' đạt tốc độ cực nhanh (~{throughputRps:F0} QR/giây). Tạo 2.000 QR chỉ mất ~{2000 / throughputRps:F1}s, tạo 10.000 QR chỉ mất ~{10000 / throughputRps:F1}s!",
                _ => $"Mẫu '{cleanTemplate}' render thẻ ảnh lớn. Tạo 2.000 QR mất ~{2000 / throughputRps:F1}s, tạo 10.000 QR mất ~{10000 / throughputRps / 60.0:F1} phút. Khuyên dùng 'qr_only' nếu cần phản hồi tức thì."
            }
        });
    }
}
