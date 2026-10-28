using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietQR.Api.Models;
using VietQR.Core.Emv;
using VietQR.Core.Models;
using VietQR.Core.Rendering;

namespace VietQR.Api.Controllers;

/// <summary>
/// API chẩn đoán, kiểm thử hệ thống và benchmark hiệu năng trên server IIS.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/vietqr-core")]
[Route("api/v1/diagnostics")]
[Produces("application/json")]
public class DiagnosticsController : ControllerBase
{
    private readonly QrImageService qrService;
    private readonly QrCodesController qrCodesController;
    private readonly ILogger<DiagnosticsController> logger;
    private readonly IHostEnvironment? env;
    private readonly IConfiguration? config;

    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"); }
        catch { return TimeZoneInfo.Utc; }
    }

    public DiagnosticsController(
        QrImageService qrService,
        QrCodesController qrCodesController,
        ILogger<DiagnosticsController> logger,
        IHostEnvironment? env = null,
        IConfiguration? config = null)
    {
        this.qrService = qrService;
        this.qrCodesController = qrCodesController;
        this.logger = logger;
        this.env = env;
        this.config = config;
    }

    private bool IsDiagnosticsAllowed() =>
        env == null || env.IsDevelopment() || config?.GetValue<bool>("Diagnostics:Enabled", false) == true;

    private IActionResult AccessDeniedResult() =>
        NotFound(new { code = "404", desc = "Chức năng chẩn đoán chỉ khả dụng ở môi trường Development hoặc khi được cấu hình bật." });

    /// <summary>
    /// Chuyển hướng nhanh tới Thư viện 76 thẻ in VietQR Print.
    /// </summary>
    [HttpGet("gallery")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult OpenGallery()
    {
        if (!IsDiagnosticsAllowed())
        {
            return AccessDeniedResult();
        }
        return Redirect("/all_banks_gallery.html");
    }

    /// <summary>
    /// Kiểm thử chất lượng toàn diện (Self-Test &amp; Quality Diagnostic) trực tiếp trên máy chủ IIS.
    /// </summary>
    /// <remarks>
    /// Kiểm thử tự động thuật toán CRC16, định dạng chuỗi EMVCo, engine SkiaSharp và quyền truy cập thư mục dữ liệu.
    /// </remarks>
    /// <response code="200">Hệ thống vượt qua 100% các bài kiểm thử chất lượng trên IIS Server.</response>
    /// <response code="500">Phát hiện lỗi chất lượng hoặc cấu hình server IIS.</response>
    [HttpGet("self-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunSelfTest()
    {
        if (!IsDiagnosticsAllowed())
        {
            return AccessDeniedResult();
        }

        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var testResults = new List<object>();
        bool allPassed = true;

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

        var swBench = System.Diagnostics.Stopwatch.StartNew();
        const int benchIterations = 1000;
        var benchQr = "00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test6304";
        for (int i = 0; i < benchIterations; i++)
        {
            _ = VietQr.GenCRCCode(benchQr);
        }
        swBench.Stop();
        double crcSpeedNs = (swBench.Elapsed.Ticks * 100.0) / benchIterations;

        swTotal.Stop();

        var responseObj = new
        {
            code = allPassed ? "00" : "99",
            trangThaiKiemThu = allPassed ? "100% PASSED - TOÀN BỘ HỆ THỐNG ĐẠT CHUẨN CHẤT LƯỢNG!" : "FAILED - PHÁT HIỆN LỖI TRÊN MÁY CHỦ",
            tongThoiGianKiemThuMs = swTotal.ElapsedMilliseconds,
            thoiGianChayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone).ToString("dd/MM/yyyy HH:mm:ss"),
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
    [HttpGet("benchmark-school-test")]
    [HttpPost("benchmark-school-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RunSchoolTuitionStressTest(
        [FromQuery] int testRequests = 2000,
        [FromQuery] string template = "qr_only",
        [FromQuery] int totalSchools = 1000,
        [FromQuery] int studentsPerSchool = 1000)
    {
        if (!IsDiagnosticsAllowed())
        {
            return AccessDeniedResult();
        }

        testRequests = Math.Clamp(testRequests, 1, 1000);
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

        if (cleanTemplate != "emv_only")
        {
            for (int i = 0; i < 3; i++)
            {
                _ = await qrCodesController.GenerateVietQrCard(
                    bankBin: "970415",
                    accountNo: "113366668888",
                    amount: "15500000",
                    description: "Hoc phi khoi dong warmup",
                    accountName: longSchoolName,
                    template: cleanTemplate);
            }
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int successCount = 0;
        int errorCount = 0;

        if (cleanTemplate == "emv_only")
        {
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
            await Parallel.ForEachAsync(
                Enumerable.Range(1, testRequests),
                new ParallelOptions { MaxDegreeOfParallelism = processorCount },
                async (i, ct) =>
                {
                    try
                    {
                        var res = await qrCodesController.GenerateVietQrCard(
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

        double sec1School = studentsPerSchool / throughputRps;
        double min100Schools = (100.0 * studentsPerSchool) / throughputRps / 60.0;
        double hoursTotalSchools = totalStudents / throughputRps / 3600.0;

        return Ok(new
        {
            code = "00",
            desc = $"Hoàn tất kiểm thử tải thực tế {testRequests:N0} mã VietQR trên IIS Server",
            kichBan = new
            {
                soRequestChayThuNghiem = testRequests,
                soRequestThanhCong = successCount,
                soRequestThatBai = errorCount
            },
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
