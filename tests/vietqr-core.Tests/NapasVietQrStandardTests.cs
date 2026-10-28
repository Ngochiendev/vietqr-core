using System.Globalization;
using System.Text.Json;
using vietqr_core.Services;
using Xunit;

namespace vietqr_core.Tests;

/// <summary>
/// Bộ kiểm thử chuẩn định dạng mã VietQR theo tài liệu NAPAS247 (QR_Format_T&C_v1.0_VN_092021.pdf).
/// </summary>
public class NapasVietQrStandardTests
{
    private static string GetProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "vietqr-core.csproj")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    [Fact]
    public void Test_Section_6_1_1_StaticQr_To_Account_MatchesOfficialNapasSpec()
    {
        // Theo tài liệu NAPAS v1.0 Mục 6.1.1 (Trang 29):
        // Chuỗi literal in nhầm độ dài STK 12 ("0112"), nhưng Bảng mô tả bên dưới ghi đúng độ dài 13 ("0113").
        // Dùng VietQr.Build() sinh chuẩn theo bảng mô tả với độ dài 13 và CRC chuẩn "9E6F":
        var vietQr = new VietQr()
            .SetBeneficiaryOrganization("970403", "0011012345678", VietQrServiceType.Account);

        var actualQr = vietQr.Build();

        Assert.True(VietQr.VerifyCRC(actualQr));
        Assert.EndsWith("63049E6F", actualQr);
        Assert.Contains("01130011012345678", actualQr);
    }

    [Fact]
    public void Test_Section_6_1_2_StaticQr_To_Card_MatchesOfficialNapasSpec()
    {
        const string expectedQr = "00020101021138600010A00000072701300006970403011697040311012345670208QRIBFTTC53037045802VN63044F52";

        var vietQr = new VietQr()
            .SetBeneficiaryOrganization("970403", "9704031101234567", VietQrServiceType.Card);

        var actualQr = vietQr.Build();

        Assert.Equal(expectedQr, actualQr);
        Assert.True(VietQr.VerifyCRC(actualQr));
        Assert.Equal("4F52", actualQr[^4..]);
    }

    [Fact]
    public void Test_Section_6_1_3_DynamicQr_To_Account_MatchesOfficialNapasSpec()
    {
        const string expectedQr = "00020101021238570010A00000072701270006970403011300110123456780208QRIBFTTA530370454061800005802VN62340107NPS68690819thanh toan don hang63042E2E";

        var vietQr = new VietQr()
            .SetBeneficiaryOrganization("970403", "0011012345678", VietQrServiceType.Account)
            .SetTransactionAmount("180000")
            .SetAdditionalData(purpose: "thanh toan don hang", billNumber: "NPS6869");

        var actualQr = vietQr.Build();

        Assert.Equal(expectedQr, actualQr);
        Assert.True(VietQr.VerifyCRC(actualQr));
        Assert.Equal("2E2E", actualQr[^4..]);
    }

    [Fact]
    public void Test_Section_6_1_4_DynamicQr_To_Card_MatchesOfficialNapasSpec()
    {
        // Theo tài liệu NAPAS v1.0 Mục 6.1.4 (Trang 32 & 33):
        // Chuỗi literal trên đầu trang in nhầm "010211" (Tĩnh), nhưng Bảng mô tả trang 32 và công thức CRC trang 33
        // ghi rõ Point of Initiation Method là "010212" (Động), kèm số tiền và CRC "A203":
        const string expectedQr = "00020101021238600010A00000072701300006970403011697040311012345670208QRIBFTTC530370454061800005802VN62340107NPS68690819thanh toan don hang6304A203";

        var vietQr = new VietQr()
            .SetBeneficiaryOrganization("970403", "9704031101234567", VietQrServiceType.Card)
            .SetTransactionAmount("180000")
            .SetAdditionalData(purpose: "thanh toan don hang", billNumber: "NPS6869");

        var actualQr = vietQr.Build();

        Assert.Equal(expectedQr, actualQr);
        Assert.True(VietQr.VerifyCRC(actualQr));
        Assert.Equal("A203", actualQr[^4..]);
    }

    [Fact]
    public void Test_FlutterProject_RealWorldSample_GeneratesValidQr()
    {
        var vietQr = new VietQr()
            .SetBeneficiaryOrganization("970422", "056789799999", VietQrServiceType.Account)
            .SetTransactionAmount("50000")
            .SetAdditionalData(purpose: "DTS001720013TKIM NGUYEN THIEN AN LOP 1 THANH TOAN HOC PHI");

        var actualQr = vietQr.Build();

        Assert.StartsWith("000201010212", actualQr);
        Assert.Contains("0006970422", actualQr);
        Assert.Contains("0112056789799999", actualQr);
        Assert.Contains("540550000", actualQr);
        Assert.Contains("6304", actualQr);
        Assert.True(VietQr.VerifyCRC(actualQr));
    }

    public static IEnumerable<object[]> GetAll256ByteValues()
    {
        for (int i = 0; i < 256; i++)
        {
            yield return new object[] { i };
        }
    }

    [Theory]
    [MemberData(nameof(GetAll256ByteValues))]
    public void Test_CrcTable_EveryByteEntry_MatchesMathematicalPolynomial(int byteIndex)
    {
        const ushort poly = 0x1021;
        ushort expected = (ushort)(byteIndex << 8);
        for (int bit = 0; bit < 8; bit++)
        {
            if ((expected & 0x8000) != 0)
                expected = (ushort)((expected << 1) ^ poly);
            else
                expected = (ushort)(expected << 1);
        }

        Assert.Equal(expected, VietQr.CrcTable[byteIndex]);
    }

    public static IEnumerable<object[]> GetAll76Banks()
    {
        var projectDir = GetProjectRoot();
        var banksJsonPath = Path.Combine(projectDir, "wwwroot", "data", "banks.json");

        using var fs = File.OpenRead(banksJsonPath);
        using var doc = JsonDocument.Parse(fs);
        var data = doc.RootElement.GetProperty("data");

        foreach (var item in data.EnumerateArray())
        {
            var bin = item.TryGetProperty("bin", out var b) ? b.GetString() : null;
            var code = item.TryGetProperty("code", out var c) ? c.GetString() : "";
            var shortName = item.TryGetProperty("shortName", out var sn) ? sn.GetString() : "";

            if (!string.IsNullOrEmpty(bin))
            {
                yield return new object[] { bin, code ?? "", shortName ?? "" };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetAll76Banks))]
    public void Test_All76Banks_LogoFileExistsAndIsValidImage(string bankBin, string bankCode, string shortName)
    {
        var projectDir = GetProjectRoot();
        var logoPath = Path.Combine(projectDir, "wwwroot", "data", "logos", $"{bankBin}.png");

        Assert.True(File.Exists(logoPath), $"Logo cho ngân hàng {shortName} ({bankCode} - {bankBin}) phải tồn tại tại {logoPath}");

        var fileInfo = new FileInfo(logoPath);
        Assert.True(fileInfo.Length > 200, $"File logo {bankBin}.png không được rỗng hoặc lỗi kích thước ({fileInfo.Length} bytes)");

        // Kiểm tra khả năng giải mã ảnh hợp lệ bằng SkiaSharp (hỗ trợ cả PNG và JPEG từ CDN VietQR)
        using var bitmap = SkiaSharp.SKBitmap.Decode(logoPath);
        Assert.NotNull(bitmap);
        Assert.True(bitmap.Width > 0 && bitmap.Height > 0, $"Logo {bankBin}.png phải có kích thước hợp lệ");
    }

    [Theory]
    [MemberData(nameof(GetAll76Banks))]
    public void Test_All76Banks_GeneratesValidQrAndCrc(string bankBin, string bankCode, string shortName)
    {
        var vietQr = new VietQr()
            .SetBeneficiaryOrganization(bankBin, "0123456789")
            .SetTransactionAmount("100000")
            .SetAdditionalData(purpose: $"test chuyen khoan {bankCode}");

        var qrCode = vietQr.Build();

        Assert.NotNull(qrCode);
        Assert.True(VietQr.VerifyCRC(qrCode), $"Mã QR sinh ra cho {shortName} ({bankBin}) phải có CRC hợp lệ");

        var extractedBin = VietQr.ExtractBinFromEmvCo(qrCode);
        Assert.Equal(bankBin, extractedBin);
    }

    public static IEnumerable<object[]> GetCrcTamperingCases()
    {
        const string baseQr = "00020101021138570010A00000072701270006970403011200110123456780208QRIBFTTA53037045802VN6304F4E5";

        for (int i = 0; i < 20; i++)
        {
            int pos = i * 4;
            if (pos >= baseQr.Length - 4) break;
            var corrupted = baseQr.ToCharArray();
            corrupted[pos] = corrupted[pos] == '9' ? '8' : '9';
            yield return new object[] { new string(corrupted), $"Đổi ký tự tại vị trí {pos}" };
        }

        yield return new object[] { baseQr[..^4] + "0000", "Sai mã CRC hoàn toàn" };
        yield return new object[] { baseQr[..^4] + "F4E4", "Lệch 1 bit CRC cuối" };
        yield return new object[] { baseQr[..^4] + "E4E5", "Lệch 1 bit CRC đầu" };
        yield return new object[] { baseQr[..^2], "Chuỗi bị cắt cụt CRC" };
        yield return new object[] { "", "Chuỗi rỗng" };
    }

    [Theory]
    [MemberData(nameof(GetCrcTamperingCases))]
    public void Test_VerifyCrc_TamperedContent_ReturnsFalse(string corruptedQr, string description)
    {
        Assert.False(VietQr.VerifyCRC(corruptedQr), $"Chuỗi bị lỗi '{description}' phải bị VerifyCRC từ chối");
    }

    [Theory]
    [InlineData("Học phí kỳ 1 năm học 2026-2027", "Hoc phi ky 1 nam hoc 2026-2027")]
    [InlineData("Đinh Tiên Hoàng Quận 1", "Dinh Tien Hoang Quan 1")]
    [InlineData("Nguyễn Thị Ánh Tuyết chuyển tiền", "Nguyen Thi Anh Tuyet chuyen tien")]
    [InlineData("Thanh toán tiền điện tháng 10/2026", "Thanh toan tien dien thang 10/2026")]
    [InlineData("CÔNG TY CỔ PHẦN CÔNG NGHỆ DTS", "CONG TY CO PHAN CONG NGHE DTS")]
    [InlineData("Đồng khởi, Bến Tre", "Dong khoi, Ben Tre")]
    [InlineData("Trường Đại học Bách Khoa", "Truong Dai hoc Bach Khoa")]
    [InlineData("Lê Văn Tám - Lớp 5A", "Le Van Tam - Lop 5A")]
    [InlineData("Ủng hộ đồng bào lũ lụt miền Trung", "Ung ho dong bao lu lut mien Trung")]
    [InlineData("Mua vé máy bay khứ hồi Hà Nội - Sài Gòn", "Mua ve may bay khu hoi Ha Noi - Sai Gon")]
    [InlineData("thanh toan hoc phi", "thanh toan hoc phi")] // ASCII thuần túy
    [InlineData("DTS001720013", "DTS001720013")] // Mã đơn hàng
    [InlineData("ĐỒNG BẰNG SÔNG CỬU LONG", "DONG BANG SONG CUU LONG")]
    [InlineData("Số nhà 123/45/6 đường Nguyễn Huệ", "So nha 123/45/6 duong Nguyen Huệ")]
    [InlineData("Vũ Trọng Phụng - Giông Tố", "Vu Trong Phung - Giong To")]
    [InlineData("Phạm Ngũ Lão, P. Phạm Ngũ Lão, Q. 1", "Pham Ngu Lao, P. Pham Ngu Lao, Q. 1")]
    [InlineData("Tạ Hiện, Hoàn Kiếm, Hà Nội", "Ta Hien, Hoan Kiem, Ha Noi")]
    [InlineData("Quốc lộ 1A, Cần Thơ", "Quoc lo 1A, Can Tho")]
    [InlineData("Võ Nguyên Giáp", "Vo Nguyen Giap")]
    [InlineData("Hồ Chí Minh", "Ho Chi Minh")]
    public void Test_VietnameseDiacriticsRemoval_DiverseCases(string raw, string expected)
    {
        var cleaned = VietQr.RemoveDiacritics(raw);
        var expectedCleaned = VietQr.RemoveDiacritics(expected);
        Assert.Equal(expectedCleaned, cleaned);

        var qr = new VietQr()
            .SetBeneficiaryOrganization("970422", "056789799999")
            .SetAdditionalData(purpose: raw)
            .Build();

        Assert.Contains(expectedCleaned[..Math.Min(expectedCleaned.Length, 25)], qr);
        Assert.True(VietQr.VerifyCRC(qr));
    }

    [Theory]
    [InlineData("MB", "03")] // Mã chữ
    [InlineData("VCB", "03")] // Mã chữ
    [InlineData("TCB", "03")] // Mã chữ
    [InlineData("ACB", "03")] // Mã chữ
    [InlineData("STB", "03")] // Mã chữ
    [InlineData("12345", "03")] // 5 số
    [InlineData("1234", "03")] // 4 số
    [InlineData("123", "03")] // 3 số
    [InlineData("9704221", "03")] // 7 số
    [InlineData("97042299", "03")] // 8 số
    [InlineData("97042A", "03")] // Có chữ lẫn số
    [InlineData("97-042", "03")] // Có ký tự đặc biệt
    [InlineData("999999", "04")] // 6 số nhưng không tồn tại trong banks.json
    [InlineData("000000", "04")] // 6 số nhưng không tồn tại trong banks.json
    [InlineData("111111", "04")] // 6 số nhưng không tồn tại trong banks.json
    public async Task Test_Controller_InvalidBankBin_ReturnsAppropriateErrorCode(string bin, string expectedCode)
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard(bin, "056789799999", "50000", "hoc phi");
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

        var json = JsonSerializer.Serialize(badRequest.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expectedCode, doc.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("01234567890123456789", "03")] // 20 ký tự (vượt 19 ký tự theo chuẩn NAPAS)
    [InlineData("012345678901234567890123456789", "03")] // 30 ký tự
    public async Task Test_Controller_InvalidAccountNo_ReturnsError03(string invalidAcc, string expectedCode)
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard("970422", invalidAcc, "50000", "hoc phi");
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

        var json = JsonSerializer.Serialize(badRequest.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expectedCode, doc.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("0", "02")] // Bằng 0
    [InlineData("-50000", "02")] // Âm
    [InlineData("abc", "02")] // Chữ
    [InlineData("!@#$", "02")] // Ký tự đặc biệt
    [InlineData("0000", "02")] // Toàn số 0
    [InlineData("-1", "02")] // Âm nhỏ nhất
    [InlineData("50000.00", "02")] // Có dấu chấm thập phân (Chuẩn VietQR từ chối)
    [InlineData("50000.0", "02")]
    [InlineData("50000.", "02")]
    [InlineData("50.000", "02")]
    [InlineData("50,000", "02")]
    [InlineData("5,0000", "02")] // Dấu phẩy ở vị trí bất kỳ
    [InlineData("99999999999999", "02")] // 14 chữ số (vượt tối đa 13 chữ số)
    public async Task Test_Controller_InvalidAmount_ReturnsError02(string invalidAmount, string expectedCode)
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard("970422", "056789799999", invalidAmount, "hoc phi");
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

        var json = JsonSerializer.Serialize(badRequest.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expectedCode, doc.RootElement.GetProperty("code").GetString());
        Assert.Equal("Invalid amount - Trường chỉ nhập số, tối đa 13 kí tự", doc.RootElement.GetProperty("desc").GetString());
    }

    [Theory]
    [InlineData("50000", "50000")]
    [InlineData("1000000", "1000000")] // 1 triệu
    [InlineData("10000000", "10000000")] // 10 triệu
    [InlineData("9999999999999", "9999999999999")] // 13 chữ số tối đa
    [InlineData("  50000  ", "50000")] // Khoảng trắng thừa
    public async Task Test_Controller_FormattedAmount_CleansSuccessfully(string inputAmount, string expectedDigits)
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard("970422", "056789799999", inputAmount, "hoc phi");
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);

        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("00", doc.RootElement.GetProperty("code").GetString());
        var qrCode = doc.RootElement.GetProperty("data").GetProperty("qrCode").GetString()!;
        Assert.Contains($"54{expectedDigits.Length:D2}{expectedDigits}", qrCode);
    }

    [Theory]
    [InlineData(null, "056789799999", "50000", "hoc phi")] // Thiếu bankBin
    [InlineData("970422", null, "50000", "hoc phi")] // Thiếu accountNo
    [InlineData("970422", "056789799999", null, "hoc phi")] // Thiếu amount
    [InlineData("970422", "056789799999", "50000", null)] // Thiếu description
    [InlineData("", "056789799999", "50000", "hoc phi")] // bankBin rỗng
    [InlineData("970422", "", "50000", "hoc phi")] // accountNo rỗng
    [InlineData("970422", "056789799999", "", "hoc phi")] // amount rỗng
    [InlineData("970422", "056789799999", "50000", "   ")] // description toàn khoảng trắng
    public async Task Test_Controller_MissingMandatoryParams_ReturnsError01(string? bin, string? acc, string? amt, string? desc)
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard(bin, acc, amt, desc);
        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

        var json = JsonSerializer.Serialize(badRequest.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("01", doc.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("123456789", "29B1")] // Vector chuẩn quốc tế CRC-16/CCITT
    [InlineData("00020101021138570010A00000072701270006970403011300110123456780208QRIBFTTA53037045802VN6304", "9E6F")] // Vector chuẩn NAPAS 6.1.1
    [InlineData("00020101021138600010A00000072701300006970403011697040311012345670208QRIBFTTC53037045802VN6304", "4F52")] // Vector chuẩn NAPAS 6.1.2
    [InlineData("00020101021238570010A00000072701270006970403011300110123456780208QRIBFTTA530370454061800005802VN62340107NPS68690819thanh toan don hang6304", "2E2E")] // Vector chuẩn NAPAS 6.1.3
    [InlineData("00020101021238600010A00000072701300006970403011697040311012345670208QRIBFTTC530370454061800005802VN62340107NPS68690819thanh toan don hang6304", "A203")] // Vector chuẩn NAPAS 6.1.4
    public void Test_Crc16Ccitt_InternationalStandardVectors(string input, string expectedHexCrc)
    {
        var actualHexCrc = VietQr.GenCRCCode(input);
        Assert.Equal(expectedHexCrc, actualHexCrc);

        if (input.EndsWith("6304"))
        {
            var fullQr = input + actualHexCrc;
            Assert.True(VietQr.VerifyCRC(fullQr));
        }
    }

    [Fact]
    public void Test_ConvertLength_SafeTwoDigits()
    {
        Assert.Equal("05", VietQr.ConvertLength(5));
        Assert.Equal("25", VietQr.ConvertLength(25));
        Assert.Equal("99", VietQr.ConvertLength(99));
        Assert.Throws<ArgumentOutOfRangeException>(() => VietQr.ConvertLength(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => VietQr.ConvertLength(150));
    }

    [Fact]
    public async Task Test_QrImageService_GenerateWithBin970422_EmbedsLogo()
    {
        var projectDir = GetProjectRoot();
        var mockEnv = new MockWebHostEnvironment(projectDir);
        var qrService = new QrImageService(mockEnv);

        var qrEmv = "00020101021238570010A0000007270127000697042201130567897999990208QRIBFTTA53037045405500005802VN62220818thanh toan hoc phi630449C3";

        var base64Url = await qrService.GenerateVietQrBase64Async(
            emvQrContent: qrEmv,
            bankName: "MBBank",
            bankBin: "970422",
            bankLogoUrl: "https://cdn.vietqr.io/img/MB.png");

        Assert.NotNull(base64Url);
        Assert.StartsWith("data:image/png;base64,", base64Url);

        var rawBase64 = base64Url.Substring("data:image/png;base64,".Length);
        var bytes = Convert.FromBase64String(rawBase64);
        Assert.True(bytes.Length > 10000, "Ảnh thẻ VietQR có kèm logo ngân hàng phải có kích thước > 10KB");
    }

    [Fact]
    public async Task Test_Controller_Valid4Params_ReturnsSuccess00()
    {
        var controller = CreateTestController();
        var result = await controller.GenerateVietQrCard("970422", "056789799999", "50000", "thanh toan hoc phi");
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);

        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("00", doc.RootElement.GetProperty("code").GetString());
        Assert.NotNull(doc.RootElement.GetProperty("data").GetProperty("qrCode").GetString());
        Assert.NotNull(doc.RootElement.GetProperty("data").GetProperty("qrDataURL").GetString());
    }

    // HELPER & MOCKS DÀNH CHO UNIT TEST

    private static vietqr_core.Controllers.VietQrCoreController CreateTestController()
    {
        var projectDir = GetProjectRoot();
        var mockEnv = new MockWebHostEnvironment(projectDir);

        var syncService = new BankSyncBackgroundService(
            new MockHttpClientFactory(),
            mockEnv,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BankSyncBackgroundService>.Instance);

        var qrService = new QrImageService(mockEnv);
        return new vietqr_core.Controllers.VietQrCoreController(
            syncService,
            qrService,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<vietqr_core.Controllers.VietQrCoreController>.Instance);
    }

    private class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private class MockWebHostEnvironment(string contentRoot) : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(contentRoot, "wwwroot");
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "vietqr-core";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}
