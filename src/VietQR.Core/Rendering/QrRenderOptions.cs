namespace VietQR.Core.Rendering;

public class QrRenderOptions
{
    public string Template { get; set; } = "compact";
    public string? BankBin { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountName { get; set; }
    public string? Amount { get; set; }
    public string? Description { get; set; }
    public int Size { get; set; } = 500;
}
