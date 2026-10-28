namespace VietQR.Core.Banks;

public sealed record BankInfo(
    string Bin,
    string Code,
    string ShortName,
    string Name,
    string Logo);
