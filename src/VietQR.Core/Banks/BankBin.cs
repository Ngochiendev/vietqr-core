namespace VietQR.Core;

public sealed record BankBin
{
    public string Value { get; }

    private BankBin(string value) => Value = value;

    public static BankBin Create(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length != 6 || !value.All(c => c >= '0' && c <= '9'))
            throw new ArgumentException("BIN phải gồm đúng 6 chữ số.", nameof(raw));
        return new BankBin(value);
    }

    public override string ToString() => Value;

    public static implicit operator string(BankBin bin) => bin.Value;
    public static implicit operator BankBin(string raw) => Create(raw);
}
