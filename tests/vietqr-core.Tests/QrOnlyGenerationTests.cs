using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.Text.Json;
using vietqr_core.Controllers;
using vietqr_core.Services;
using Xunit;

namespace vietqr_core.Tests;

public class QrOnlyGenerationTests
{
    private const string SampleEmvQr = "00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test63048E1D";

    [Fact]
    public void GenerateQrOnlyBase64_ReturnsValidBase64DataUrl()
    {
        var qrService = new QrImageService();
        string dataUrl = qrService.GenerateQrOnlyBase64(SampleEmvQr, 500);

        Assert.NotNull(dataUrl);
        Assert.StartsWith("data:image/png;base64,", dataUrl);

        string rawBase64 = dataUrl.Replace("data:image/png;base64,", "");
        byte[] bytes = Convert.FromBase64String(rawBase64);
        Assert.NotEmpty(bytes);

        using var bitmap = SKBitmap.Decode(bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(500, bitmap.Width);
        Assert.Equal(500, bitmap.Height);
    }

    [Theory]
    [InlineData("qr_only")]
    [InlineData("QR_ONLY")]
    public async Task GenerateVietQrBase64Async_WithQrOnlyTemplate_ReturnsSquareQr(string template)
    {
        var qrService = new QrImageService();
        string dataUrl = await qrService.GenerateVietQrBase64Async(SampleEmvQr, template: template);

        Assert.StartsWith("data:image/png;base64,", dataUrl);

        byte[] bytes = Convert.FromBase64String(dataUrl.Replace("data:image/png;base64,", ""));
        using var bmp = SKBitmap.Decode(bytes);
        Assert.Equal(500, bmp.Width);
        Assert.Equal(500, bmp.Height);
    }

    [Theory]
    [InlineData("compact")]
    [InlineData(null)]
    public async Task GenerateVietQrBase64Async_WithCompactTemplate_ReturnsFullCard(string? template)
    {
        var qrService = new QrImageService();
        string dataUrl = await qrService.GenerateVietQrBase64Async(
            SampleEmvQr,
            bankName: "MBBank",
            bankBin: "970422",
            template: template);

        Assert.StartsWith("data:image/png;base64,", dataUrl);

        byte[] bytes = Convert.FromBase64String(dataUrl.Replace("data:image/png;base64,", ""));
        using var bmp = SKBitmap.Decode(bytes);
        Assert.Equal(600, bmp.Width);
        Assert.Equal(585, bmp.Height);
    }

    [Theory]
    [InlineData("print")]
    [InlineData("PRINT")]
    public async Task GenerateVietQrBase64Async_WithPrintTemplate_ReturnsCardWithDetails(string template)
    {
        var qrService = new QrImageService();
        string dataUrl = await qrService.GenerateVietQrBase64Async(
            SampleEmvQr,
            bankName: "VietinBank",
            bankBin: "970415",
            bankFullName: "Ngân hàng TMCP Công thương Việt Nam",
            accountNo: "113366668888",
            accountName: "QUY VAC XIN COVID",
            amount: "790000",
            description: "Dong gop quy vac xin",
            template: template);

        Assert.StartsWith("data:image/png;base64,", dataUrl);

        byte[] bytes = Convert.FromBase64String(dataUrl.Replace("data:image/png;base64,", ""));
        using var bmp = SKBitmap.Decode(bytes);
        Assert.Equal(600, bmp.Width);
        // Height should be larger than compact (585) to accommodate the text lines
        Assert.True(bmp.Height > 700);
    }

    [Fact]
    public async Task GenerateAll76BanksPrintCards()
    {
        // === CẤU HÌNH THÔNG TIN CHUNG (Áp dụng khi không chỉ định STK riêng) ===
        var defaultAccountNo = "113366668888";
        var defaultAccountName = "QUY VAC XIN COVID";
        var defaultAmount = "10000"; // Mức tối thiểu chuẩn chuyển khoản liên ngân hàng Napas247 (10.000 VNĐ)
        var defaultDescription = "Dong gop quy vac xin";

        // === 76 CHỖ ĐỂ GÁN 76 SỐ TÀI KHOẢN VÀ TÊN CHỦ TK TÙY CHỈNH THEO TỪNG NGÂN HÀNG ===
        // Lưu ý quan trọng: Số tài khoản BẮT BUỘC đặt trong nháy kép "" để giữ nguyên các số 0 ở đầu (VD: "0057108879798668", "0838539629")
        var bankAccounts = new Dictionary<string, (string AccountNo, string AccountName)>
        {
            // #01. VietinBank (ICB - BIN: 970415)
            ["970415"] = ("888860950666", "NGUYEN DUY HIEU"),
            // #02. Vietcombank (VCB - BIN: 970436)
            ["970436"] = ("5520999999", "HO DANG VINH"),
            // #03. BIDV (BIDV - BIN: 970418)
            ["970418"] = ("6800402782", "TANG NHAN QUI"),
            // #04. Agribank (VBA - BIN: 970405)
            ["970405"] = ("2200205843192", "NGUYEN TIEN HUONG"),
            // #05. OCB (OCB - BIN: 970448)
            ["970448"] = ("0057108879798668", "NGUYEN TRAN THIEN PHUC"),
            // #06. MBBank (MB - BIN: 970422)
            ["970422"] = ("0838539629", "TRAN QUOC VY"),
            // #07. Techcombank (TCB - BIN: 970407)
            ["970407"] = ("3357553403", "NGUYEN DUC HUNG"),
            // #08. ACB (ACB - BIN: 970416)
            ["970416"] = ("18301851", "NGO VAN TUAN"),
            // #09. VPBank (VPB - BIN: 970432)
            ["970432"] = ("0382908829", "NGUYEN CONG VIET"),
            // #10. TPBank (TPB - BIN: 970423)
            ["970423"] = ("83286868886", "NGUYEN THI THUY VY"),
            // #11. Sacombank (STB - BIN: 970403)
            ["970403"] = ("215102009", "LUONG PHU QUOC"),
            // #12. HDBank (HDB - BIN: 970437)
            ["970437"] = ("002704070014601", "HD SAIGON"),
            // #13. VietCapitalBank (VCCB - BIN: 970454)
            ["970454"] = ("90546139001", "NGO NGOC LY"),
            // #14. SCB (SCB - BIN: 970429)
            ["970429"] = ("49864542950", "NGUYEN THI LINH"),
            // #15. VIB (VIB - BIN: 970441)
            ["970441"] = ("388388386", "TRAN THI TUYET NHUNG"),
            // #16. SHB (SHB - BIN: 970443)
            ["970443"] = ("1021920896", "HOANG VAN CUONG"),
            // #17. Eximbank (EIB - BIN: 970431)
            ["970431"] = ("100443918", "TRINH NHAT KHANH"),
            // #18. MSB (MSB - BIN: 970426)
            ["970426"] = ("80003678769", "HOANG THU TRA"),
            // #19. CAKE (CAKE - BIN: 546034)
            ["546034"] = ("0989680851", "VO NGOC HIEN"),
            // #20. Ubank (Ubank - BIN: 546035)
            ["546035"] = (defaultAccountNo, defaultAccountName),
            // #21. Timo (TIMO - BIN: 963388)
            ["963388"] = ("0822052467", "BUI DUC VINH"),
            // #22. ViettelMoney (VTLMONEY - BIN: 971005)
            ["971005"] = ("0989680851", "VO NGOC HIEN"),
            // #23. VNPTMoney (VNPTMONEY - BIN: 971011)
            ["971011"] = ("0914543663", "AN VU NHAT MINH"),
            // #24. SaigonBank (SGICB - BIN: 970400)
            ["970400"] = ("000170406009426", "NGUYEN DANG TRIEU"),
            // #25. BacABank (BAB - BIN: 970409)
            ["970409"] = ("195001060005908", "VU MINH HOANG"),
            // #26. PVcomBank Pay (PVDB - BIN: 971133)
            ["971133"] = (defaultAccountNo, defaultAccountName),
            // #27. MoMo (momo - BIN: 971025)
            ["971025"] = ("0989680851", "VO NGOC HIEN"),
            // #28. PVcomBank (PVCB - BIN: 970412)
            ["970412"] = ("109001535243", "DANG DUC TOAN"),
            // #29. MBV (MBV - BIN: 970414)
            ["970414"] = ("101099999999", "NGUYEN TRAN CHINH"),
            // #30. NCB (NCB - BIN: 970419)
            ["970419"] = ("103245021929", "PHAN VAN DAT"),
            // #31. ShinhanBank (SHBVN - BIN: 970424)
            ["970424"] = ("0329919761", "NGUYEN VAN VUI"),
            // #32. ABBANK (ABB - BIN: 970425)
            ["970425"] = ("0964750147", "PHAM THI THUY"),
            // #33. VietABank (VAB - BIN: 970427)
            ["970427"] = ("00550367", "PHAM THUC DINH"),
            // #34. NamABank (NAB - BIN: 970428)
            ["970428"] = ("298267733912345", "LAM CONG MINH"),
            // #35. PGBank (PGB - BIN: 970430)
            ["970430"] = ("0552652254266", "TRIEU DUY MANH"),
            // #36. VietBank (VIETBANK - BIN: 970433)
            ["970433"] = ("000001111970", "TRAN VAN TUYEN"),
            // #37. BaoVietBank (BVB - BIN: 970438)
            ["970438"] = ("99ZP24290M46240583", "MAI PHUC TRI"),
            // #38. SeABank (SEAB - BIN: 970440)
            ["970440"] = ("1010822222", "TRAN THI NGOC ANH"),
            // #39. COOPBANK (COOPBANK - BIN: 970446)
            ["970446"] = ("8900000000462012", "QUY TIN DUNG NHAN DAN LONG HUNG A"),
            // #40. LPBank (LPB - BIN: 970449)
            ["970449"] = ("031116140001", "QUANG MANH CUONG"),
            // #41. KienLongBank (KLB - BIN: 970452)
            ["970452"] = ("10164545655446132", "GATE"),
            // #42. KBank (KBank - BIN: 668888)
            ["668888"] = ("106001644030", "VU HONG HAI"),
            // #43. KEBHanaHCM (KEBHANAHCM - BIN: 970466)
            ["970466"] = (defaultAccountNo, defaultAccountName),
            // #44. KEBHANAHN (KEBHANAHN - BIN: 970467)
            ["970467"] = (defaultAccountNo, defaultAccountName),
            // #45. MAFC (MAFC - BIN: 977777)
            ["977777"] = (defaultAccountNo, defaultAccountName),
            // #46. VBSP (VBSP - BIN: 999888)
            ["999888"] = ("1000007062881486", "BON NIENG HA ANG"),
            // #47. BNP HN (BNP HN - BIN: 963668)
            ["963668"] = (defaultAccountNo, defaultAccountName),
            // #48. BIDC (BIDC - BIN: 555666)
            ["555666"] = ("2120000040343", defaultAccountName),
            // #49. BNP HCM (BNP HCM - BIN: 963666)
            ["963666"] = (defaultAccountNo, defaultAccountName),
            // #50. ZaloPay (ZALOPAY - BIN: 971101)
            ["971101"] = ("0989680851", "VO NGOC HIEN"),
            // #51. BOCHK (BOCHK - BIN: 963688)
            ["963688"] = (defaultAccountNo, defaultAccountName),
            // #52. BSP (BSP - BIN: 970465)
            ["970465"] = (defaultAccountNo, defaultAccountName),
            // #53. CUBHCM (CUBHCM - BIN: 168999)
            ["168999"] = (defaultAccountNo, defaultAccountName),
            // #54. ESUN (ESUN - BIN: 963686)
            ["963686"] = (defaultAccountNo, defaultAccountName),
            // #55. ICBC (ICBC - BIN: 963669)
            ["963669"] = (defaultAccountNo, defaultAccountName),
            // #56. MVAS (MVAS - BIN: 971032)
            ["971032"] = (defaultAccountNo, defaultAccountName),
            // #57. TNEX (TNEX - BIN: 963326)
            ["963326"] = ("88898019374235", "TRINH NHAT KHANH"),
            // #58. HSBC (HSBC - BIN: 458761)
            ["458761"] = ("202238747001", "JOHNSTON SYLVIA MARGARET"),
            // #59. HongLeong (HLBVN - BIN: 970442)
            ["970442"] = ("101010000011764", defaultAccountName),
            // #60. IBKHN (IBK - HN - BIN: 970455)
            ["970455"] = (defaultAccountNo, defaultAccountName),
            // #61. IBKHCM (IBK - HCM - BIN: 970456)
            ["970456"] = (defaultAccountNo, defaultAccountName),
            // #62. IndovinaBank (IVB - BIN: 970434)
            ["970434"] = ("6416546001", "NGUYEN TRUNG HIEU"),
            // #63. Vikki (Vikki - BIN: 970406)
            ["970406"] = ("979495313", "HO THI THU BINH"),
            // #64. Nonghyup (NHB HN - BIN: 801011)
            ["801011"] = (defaultAccountNo, defaultAccountName),
            // #65. DBSBank (DBS - BIN: 796500)
            ["796500"] = ("30000467189", "DBS BANK LTD.-HO CHI MINH CITY BRAN..."),
            // #66. PublicBank (PBVN - BIN: 970439)
            ["970439"] = ("0600140000342", "DANIAL AZFAR ZOW BIN ABDULLAH"),
            // #67. StandardChartered (SCVN - BIN: 970410)
            ["970410"] = ("90154278802", "QUY NHI DONG LIEN HOP QUOC TAI VIET NAM"),
            // #68. Citibank (CITIBANK - BIN: 533948)
            ["533948"] = ("0301283016", "CTY TNHH BAO HIEM NHAN THO CHUNBB..."),
            // #69. CIMB (CIMB - BIN: 422589)
            ["422589"] = ("00336511204396", "HUYNH PHUC HIEN"),
            // #70. UnitedOverseas (UOB - BIN: 970458)
            ["970458"] = ("1043506531", "DO THANH TUAN"),
            // #71. CBBank (CBB - BIN: 970444)
            ["970444"] = (defaultAccountNo, defaultAccountName),
            // #72. GPBank (GPB - BIN: 970408)
            ["970408"] = ("00100004080038", "NGUYEN THI MINH THAO"),
            // #73. VRB (VRB - BIN: 970421)
            ["970421"] = ("152710000311092", "TRAN THI NGOC HIEN"),
            // #74. Woori (WVN - BIN: 970457)
            ["970457"] = ("PNE00008166", "PINE TREE SECURITIES NGUYEN PHUONG..."),
            // #75. KookminHN (KBHN - BIN: 970462)
            ["970462"] = (defaultAccountNo, defaultAccountName),
            // #76. KookminHCM (KBHCM - BIN: 970463)
            ["970463"] = (defaultAccountNo, defaultAccountName),
        };

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? testsDir = null;
        string? banksJsonPath = null;

        while (dir != null)
        {
            var pJson = Path.Combine(dir.FullName, "wwwroot", "data", "banks.json");
            if (File.Exists(pJson) && banksJsonPath == null) banksJsonPath = pJson;

            var pTests = Path.Combine(dir.FullName, "tests");
            if (Directory.Exists(pTests) && Directory.Exists(Path.Combine(pTests, "vietqr-core.Tests")) && testsDir == null)
            {
                testsDir = pTests;
            }
            dir = dir.Parent;
        }

        testsDir ??= Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tests"));
        Assert.True(File.Exists(banksJsonPath), $"Không tìm thấy banks.json tại: {banksJsonPath}");

        var json = await File.ReadAllTextAsync(banksJsonPath);
        using var doc = JsonDocument.Parse(json);
        var banksArray = doc.RootElement.GetProperty("data").EnumerateArray().ToList();

        var qrService = new QrImageService();
        var htmlItems = new List<string>();

        int index = 1;
        foreach (var item in banksArray)
        {
            var bin = item.TryGetProperty("bin", out var bp) ? bp.GetString() ?? "" : "";
            var code = item.TryGetProperty("code", out var cp) ? cp.GetString() ?? "" : "";
            var shortName = item.TryGetProperty("shortName", out var snp) ? snp.GetString() ?? code : code;
            var name = item.TryGetProperty("name", out var np) ? np.GetString() ?? shortName : shortName;
            var logo = item.TryGetProperty("logo", out var lp) ? lp.GetString() : null;

            if (string.IsNullOrEmpty(bin)) continue;

            // Lấy STK và Tên chủ TK riêng của ngân hàng này nếu đã cấu hình, nếu không dùng mặc định
            var targetAccountNo = defaultAccountNo;
            var targetAccountName = defaultAccountName;

            if (bankAccounts.TryGetValue(bin, out var customAcc))
            {
                var accNoStr = customAcc.AccountNo?.ToString();
                if (!string.IsNullOrWhiteSpace(accNoStr)) targetAccountNo = accNoStr;
                if (!string.IsNullOrWhiteSpace(customAcc.AccountName)) targetAccountName = customAcc.AccountName;
            }

            var qrText = new VietQr()
                .SetBeneficiaryOrganization(bin, targetAccountNo)
                .SetTransactionAmount(defaultAmount)
                .SetAdditionalDataFieldTemplate(defaultDescription)
                .Build();

            var dataUrl = await qrService.GenerateVietQrBase64Async(
                qrText,
                bankName: shortName,
                bankBin: bin,
                bankLogoUrl: logo,
                template: "print",
                accountNo: targetAccountNo,
                accountName: targetAccountName,
                amount: defaultAmount,
                description: defaultDescription,
                bankFullName: name);

            htmlItems.Add($@"
            <div class='card' data-search='{code} {shortName} {bin} {name} {targetAccountNo} {targetAccountName}'>
                <div class='badge'>#{index:D2} - {code} ({bin})</div>
                <div class='bank-title'>{shortName}</div>
                <div class='bank-sub'>{name}</div>
                <img src='{dataUrl}' alt='{shortName}' loading='lazy' />
            </div>");

            index++;
        }

        // Tạo file HTML độc lập chứa trực tiếp dữ liệu ảnh base64 (không phụ thuộc file ngoài hay cache)
        var htmlContent = $@"<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <title>Thư viện 76 Mã VietQR Print - Tất cả ngân hàng</title>
    <style>
        * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        body {{ background: #f0f4f8; color: #102a43; padding: 24px; }}
        header {{ text-align: center; margin-bottom: 24px; }}
        h1 {{ font-size: 28px; color: #003B70; margin-bottom: 8px; }}
        p {{ color: #486581; font-size: 15px; }}
        .info-box {{ display: inline-block; background: #e0f2fe; color: #0369a1; padding: 8px 16px; border-radius: 8px; font-size: 14px; margin-top: 8px; }}
        .search-box {{ max-width: 500px; margin: 16px auto; position: relative; }}
        input {{ width: 100%; padding: 12px 18px; border-radius: 24px; border: 2px solid #cbd2d9; font-size: 16px; outline: none; }}
        input:focus {{ border-color: #003B70; }}
        .grid {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 20px; max-width: 1400px; margin: 0 auto; }}
        .card {{ background: white; border-radius: 16px; padding: 16px; box-shadow: 0 4px 12px rgba(0,0,0,0.06); text-align: center; transition: transform 0.2s; }}
        .card:hover {{ transform: translateY(-4px); box-shadow: 0 8px 24px rgba(0,0,0,0.12); }}
        .badge {{ display: inline-block; background: #e0f2fe; color: #0369a1; padding: 4px 10px; border-radius: 12px; font-size: 13px; font-weight: 700; margin-bottom: 6px; }}
        .bank-title {{ font-size: 18px; font-weight: 700; color: #003B70; }}
        .bank-sub {{ font-size: 12px; color: #627d98; margin-bottom: 12px; height: 32px; overflow: hidden; }}
        img {{ width: 100%; max-width: 300px; height: auto; border-radius: 10px; border: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <header>
        <h1>Thư viện 76 Mã VietQR Chuẩn In (Template: Print)</h1>
        <p>Mã QR được tự động tạo động và nhúng trực tiếp dữ liệu ảnh Base64</p>
        <div class='info-box'>
            Chủ TK: <b>{defaultAccountName}</b> | Số tiền: <b>{defaultAmount} đ</b> | STK Mặc định: <b>{defaultAccountNo}</b>
        </div>
        <div class='search-box'>
            <input type='text' id='search' placeholder='Tìm nhanh theo tên, mã BIN (VD: 970422, MB, VCB...)' oninput='filterBanks()'>
        </div>
    </header>
    <div class='grid' id='grid'>
        {string.Join("\n", htmlItems)}
    </div>
    <script>
        function filterBanks() {{
            const query = document.getElementById('search').value.toLowerCase();
            document.querySelectorAll('.card').forEach(card => {{
                const search = card.getAttribute('data-search').toLowerCase();
                card.style.display = search.includes(query) ? 'block' : 'none';
            }});
        }}
    </script>
</body>
</html>";

        var htmlFilePath = Path.Combine(testsDir, "all_banks_gallery.html");
        await File.WriteAllTextAsync(htmlFilePath, htmlContent);

        // Xuất trực tiếp vào wwwroot để IIS Server tự động phục vụ qua web URL
        var projectRoot = Path.GetDirectoryName(testsDir);
        if (!string.IsNullOrEmpty(projectRoot))
        {
            var wwwrootGallery = Path.Combine(projectRoot, "wwwroot", "all_banks_gallery.html");
            if (Directory.Exists(Path.Combine(projectRoot, "wwwroot")))
            {
                await File.WriteAllTextAsync(wwwrootGallery, htmlContent);
            }

            var publishWwwrootGallery = Path.Combine(projectRoot, "publish", "wwwroot", "all_banks_gallery.html");
            if (Directory.Exists(Path.Combine(projectRoot, "publish", "wwwroot")))
            {
                await File.WriteAllTextAsync(publishWwwrootGallery, htmlContent);
            }
        }
    }

    [Fact]
    public async Task Controller_WithInvalidTemplate_ReturnsCode05()
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard(
            bankBin: "970422",
            accountNo: "056789799999",
            amount: "50000",
            description: "test",
            template: "invalid_template");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("05", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Controller_WithQrOnlyTemplate_ReturnsSuccessWithQrOnlyDataUrl()
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard(
            bankBin: "970422",
            accountNo: "056789799999",
            amount: "50000",
            description: "test",
            template: "qr_only");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("00", doc.RootElement.GetProperty("code").GetString());
        var data = doc.RootElement.GetProperty("data");
        Assert.NotNull(data.GetProperty("qrCode").GetString());
        Assert.NotNull(data.GetProperty("qrDataURL").GetString());
    }

    [Fact]
    public async Task Controller_WithPrintTemplate_ReturnsSuccessWithPrintDataUrl()
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard(
            bankBin: "970415",
            accountNo: "113366668888",
            accountName: "QUY VAC XIN COVID",
            amount: "790000",
            description: "dong qop quy vac xin",
            template: "print");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("00", doc.RootElement.GetProperty("code").GetString());
        var data = doc.RootElement.GetProperty("data");
        Assert.NotNull(data.GetProperty("qrCode").GetString());
        Assert.NotNull(data.GetProperty("qrDataURL").GetString());
    }

    [Fact]
    public async Task Controller_RunSelfTest_ReturnsOkAndPassesAllChecks()
    {
        var controller = CreateTestController();
        var result = await controller.RunSelfTest();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("00", doc.RootElement.GetProperty("code").GetString());
        Assert.Contains("100% PASSED", doc.RootElement.GetProperty("trangThaiKiemThu").GetString());
    }

    [Fact]
    public async Task Controller_RunSchoolTuitionStressTest_ReturnsValidBenchmarkReport()
    {
        var controller = CreateTestController();
        var result = await controller.RunSchoolTuitionStressTest(
            testRequests: 10,
            template: "print",
            totalSchools: 1000,
            studentsPerSchool: 1000);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("00", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal(10, doc.RootElement.GetProperty("kichBan").GetProperty("soRequestChayThuNghiem").GetInt32());
        Assert.Equal(10, doc.RootElement.GetProperty("kichBan").GetProperty("soRequestThanhCong").GetInt32());
    }

    [Fact]
    public void Controller_OpenGallery_ReturnsRedirectToAllBanksGallery()
    {
        var controller = CreateTestController();
        var result = controller.OpenGallery();

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/all_banks_gallery.html", redirectResult.Url);
    }

    private static VietQrCoreController CreateTestController()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        string? projectRoot = null;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "vietqr-core.csproj")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        var env = new TestWebHostEnvironment(projectRoot ?? baseDir);
        var syncService = new BankSyncBackgroundService(
            new TestHttpClientFactory(),
            env,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BankSyncBackgroundService>.Instance);
        var qrService = new QrImageService(env);

        return new VietQrCoreController(
            syncService,
            qrService,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<VietQrCoreController>.Instance);
    }

    [Fact]
    public void DebugFooterVisualBounds()
    {
        var napasPath = Path.Combine(AppContext.BaseDirectory, "../../../../../wwwroot/data/napas247.png");
        var bankPath = Path.Combine(AppContext.BaseDirectory, "../../../../../wwwroot/data/logos/970415.png");

        using var napasBmp = SKBitmap.Decode(napasPath);
        using var bankBmp = SKBitmap.Decode(bankPath);

        Assert.NotNull(napasBmp);
        Assert.NotNull(bankBmp);

        // Analyze non-background pixels
        static (SKRect Bounds, float CenterY, float BottomY, float BaselineY) Analyze(SKBitmap bmp)
        {
            int minX = bmp.Width, maxX = 0;
            int minY = bmp.Height, maxY = 0;
            double weightedY = 0;
            long totalWeight = 0;
            int[] rowCounts = new int[bmp.Height];

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var p = bmp.GetPixel(x, y);
                    bool isBg = p.Alpha <= 15 || (p.Red >= 248 && p.Green >= 248 && p.Blue >= 248);
                    if (!isBg)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        weightedY += y;
                        totalWeight++;
                        rowCounts[y]++;
                    }
                }
            }
            float centerY = totalWeight > 0 ? (float)(weightedY / totalWeight) : (minY + maxY) / 2f;
            return (new SKRect(minX, minY, maxX + 1, maxY + 1), centerY, maxY + 1, maxY + 1);
        }

        var (napasRect, napasCenterY, napasBottom, _) = Analyze(napasBmp);
        var (bankRect, bankCenterY, bankBottom, _) = Analyze(bankBmp);

        // Target visual height = 25
        float targetVisualHeight = 25f;
        float scale = targetVisualHeight / napasRect.Height;
        float bankScale = scale; // Because bankBmp is 831x311 (>=800 && >= 300)

        int napasW = (int)(napasRect.Width * scale);
        int napasH = (int)(napasRect.Height * scale);
        int bankW = (int)(bankRect.Width * bankScale);
        int bankH = (int)(bankRect.Height * bankScale);

        int footerCenterY = 550; // isPrint

        float napasVisualOffset = napasCenterY - napasRect.Top;
        int napasY = (int)Math.Round(footerCenterY - (napasVisualOffset * scale));
        int napasRenderBottom = napasY + napasH;

        float bankVisualOffset = bankCenterY - bankRect.Top;
        int bY = (int)Math.Round(footerCenterY - (bankVisualOffset * bankScale));
        int bankRenderBottom = bY + bankH;

        Assert.True(napasW > 0 && bankW > 0);
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private class TestWebHostEnvironment(string contentRoot) : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "vietqr-core";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}
