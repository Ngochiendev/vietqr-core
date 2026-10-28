namespace VietQR.Core;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new("", "");

    public bool IsEmpty => string.IsNullOrEmpty(Code);
}
