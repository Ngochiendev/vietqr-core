namespace VietQR.Core;

public sealed class CompactTemplate : IQrTemplate
{
    private readonly QrImageService _service;

    public CompactTemplate(QrImageService service)
    {
        _service = service;
    }

    public string Name => "compact";

    public Task<byte[]> RenderAsync(string emvPayload, QrRenderOptions options, CancellationToken ct = default)
    {
        return _service.RenderPngBytesAsync(
            emvPayload,
            bankBin: options.BankBin,
            template: "compact",
            accountNo: options.AccountNo,
            accountName: options.AccountName,
            amount: options.Amount is > 0
                ? options.Amount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null,
            description: options.Description);
    }
}
