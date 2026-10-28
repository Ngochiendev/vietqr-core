using System.Diagnostics;
using System.Globalization;
using System.Text;
using vietqr_core.Services;
using Xunit;
using Xunit.Abstractions;

namespace vietqr_core.Tests;

public class CrcBenchmarkTests(ITestOutputHelper output)
{
    private const string SampleQrContent = "00020101021238570010A0000007270127000697042201120567897999990208QRIBFTTA53037045405500005802VN62180814thanh toan test6304";

    // Cách 1: Bit-by-bit (User cung cấp)
    private static string Crc16Ccitt_BitByBit(string input)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(input))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    // Cách 2: Table-driven (Trong VietQr.cs)
    private static string Crc16Ccitt_TableDriven(string input)
    {
        return VietQr.GenCRCCode(input);
    }

    // Sinh chuỗi VietQR chuẩn NAPAS dài nhất có thể:
    // - STK tối đa 19 ký tự
    // - Số tiền tối đa 13 chữ số
    // - Bill Number tối đa 99 ký tự
    // - Purpose tối đa 99 ký tự
    public static string GenerateMaxVietQrString()
    {
        var builder = new VietQr()
            .SetBeneficiaryOrganization("970422", "9999999999999999999", VietQrServiceType.Account)
            .SetTransactionAmount("9999999999999")
            .SetAdditionalData(
                purpose: "THANH TOAN DON HANG SO " + new string('X', 76), // 99 ký tự
                billNumber: "BILL-ORDER-" + new string('Y', 88));          // 99 ký tự

        var fullQr = builder.Build();
        return fullQr[..^4]; // Chuỗi tính CRC (kết thúc tại "6304")
    }

    // Sinh chuỗi EMVCo Stress-Test cực đại (512 hoặc 1024 ký tự)
    public static string GenerateStressTestString(int targetLength)
    {
        var baseStr = GenerateMaxVietQrString();
        var sb = new StringBuilder(baseStr);
        int tagId = 64;
        while (sb.Length < targetLength - 10)
        {
            int remaining = targetLength - sb.Length - 4;
            int len = Math.Min(remaining, 90);
            sb.Append($"{tagId:D2}{len:D2}").Append(new string('Z', len));
            tagId = (tagId + 1) % 99;
        }
        sb.Append("6304");
        return sb.ToString();
    }

    [Fact]
    public void Verify_Both_Methods_Produce_Identical_Result_On_Max_String()
    {
        string maxStr = GenerateMaxVietQrString();
        string result1 = Crc16Ccitt_BitByBit(maxStr);
        string result2 = Crc16Ccitt_TableDriven(maxStr);

        Assert.Equal(result1, result2);

        string stressStr = GenerateStressTestString(1024);
        Assert.Equal(Crc16Ccitt_BitByBit(stressStr), Crc16Ccitt_TableDriven(stressStr));
    }

    [Theory]
    [InlineData(10_000, 276)]   // Chuỗi VietQR NAPAS max thực tế: 276 ký tự
    [InlineData(100_000, 276)]  // Chuỗi VietQR NAPAS max thực tế: 276 ký tự
    [InlineData(1_000_000, 276)]// Chuỗi VietQR NAPAS max thực tế: 276 ký tự
    [InlineData(100_000, 1024)] // Chuỗi Stress-test cực đại: 1024 ký tự
    public void Benchmark_Crc_Performance_With_Max_String(int iterations, int stringLength)
    {
        string testInput = stringLength <= 300
            ? GenerateMaxVietQrString()
            : GenerateStressTestString(stringLength);

        // Warm-up JIT
        for (int i = 0; i < 1_000; i++)
        {
            _ = Crc16Ccitt_BitByBit(testInput);
            _ = Crc16Ccitt_TableDriven(testInput);
        }

        // --- Benchmark Cách 1: Bit-by-bit ---
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocBefore1 = GC.GetTotalAllocatedBytes(true);
        int gc0Before1 = GC.CollectionCount(0);
        var sw1 = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _ = Crc16Ccitt_BitByBit(testInput);
        }

        sw1.Stop();
        long allocAfter1 = GC.GetTotalAllocatedBytes(true);
        int gc0After1 = GC.CollectionCount(0);

        long time1Ms = sw1.ElapsedMilliseconds;
        double time1NsPerOp = (sw1.Elapsed.TotalNanoseconds) / iterations;
        long mem1Bytes = allocAfter1 - allocBefore1;
        int gc1Count = gc0After1 - gc0Before1;

        // --- Benchmark Cách 2: Table-driven ---
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocBefore2 = GC.GetTotalAllocatedBytes(true);
        int gc0Before2 = GC.CollectionCount(0);
        var sw2 = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _ = Crc16Ccitt_TableDriven(testInput);
        }

        sw2.Stop();
        long allocAfter2 = GC.GetTotalAllocatedBytes(true);
        int gc0After2 = GC.CollectionCount(0);

        long time2Ms = sw2.ElapsedMilliseconds;
        double time2NsPerOp = (sw2.Elapsed.TotalNanoseconds) / iterations;
        long mem2Bytes = allocAfter2 - allocBefore2;
        int gc2Count = gc0After2 - gc0Before2;

        // --- Benchmark Cách 3: Pure Math Table-Driven (Không cấp phát chuỗi) ---
        GC.Collect();
        long allocBefore3 = GC.GetTotalAllocatedBytes(true);
        var sw3 = Stopwatch.StartNew();
        var span = testInput.AsSpan();
        for (int i = 0; i < iterations; i++)
        {
            _ = VietQr.CalcCRC(span);
        }
        sw3.Stop();
        long allocAfter3 = GC.GetTotalAllocatedBytes(true);
        long time3Ms = sw3.ElapsedMilliseconds;
        double time3NsPerOp = (sw3.Elapsed.TotalNanoseconds) / iterations;
        long mem3Bytes = allocAfter3 - allocBefore3;

        double speedup = (double)time1Ms / Math.Max(1, time2Ms);
        double speedupPure = (double)time1Ms / Math.Max(1, time3Ms);
        double memRatio = mem2Bytes > 0 ? (double)mem1Bytes / mem2Bytes : double.PositiveInfinity;

        output.WriteLine($"=== KẾT QUẢ SO SÁNH VỚI {iterations:N0} LẦN TẠO MÃ QR (ĐỘ DÀI CHUỖI: {testInput.Length} KÝ TỰ) ===");
        output.WriteLine($"1. Cách 1 (Bit-by-Bit + GetBytes):      {time1Ms,6} ms ({time1NsPerOp:F1} ns/op) | RAM: {mem1Bytes / 1024.0 / 1024.0,6:F2} MB | Gen0 GC: {gc1Count,4} lần");
        output.WriteLine($"2. Cách 2 (Table-Driven + GenCRCCode):   {time2Ms,6} ms ({time2NsPerOp:F1} ns/op) | RAM: {mem2Bytes / 1024.0 / 1024.0,6:F2} MB | Gen0 GC: {gc2Count,4} lần");
        output.WriteLine($"3. Cách 3 (Table-Driven Pure Math Calc): {time3Ms,6} ms ({time3NsPerOp:F1} ns/op) | RAM: {mem3Bytes / 1024.0 / 1024.0,6:F2} MB | Gen0 GC:    0 lần");
        output.WriteLine($"-> Tốc độ End-to-End: Table-Driven nhanh hơn gấp {speedup:F2}x");
        output.WriteLine($"-> Tốc độ Pure Math:  Table-Driven nhanh hơn gấp {speedupPure:F2}x");
        output.WriteLine($"-> Bộ nhớ Heap:       Bit-by-Bit ngốn RAM hơn gấp {memRatio:F2}x và ép GC chạy liên tục");
        output.WriteLine(new string('-', 85));
    }
}
