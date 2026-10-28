namespace VietQR.Core;

public sealed class PrintTemplate : IQrTemplate
{
    private readonly QrImageService _service;

    public PrintTemplate(QrImageService service)
    {
        _service = service;
    }

    public string Name => "print";

    public Task<byte[]> RenderAsync(string emvPayload, QrRenderOptions options, CancellationToken ct = default)
    {
        return _service.RenderPngBytesAsync(
            emvPayload,
            bankBin: options.BankBin,
            template: "print",
            accountNo: options.AccountNo,
            accountName: options.AccountName,
            amount: options.Amount is > 0
                ? options.Amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null,
            description: options.Description);
    }
}
