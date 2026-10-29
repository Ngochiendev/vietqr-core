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
        options.Template = "print";
        return _service.RenderBytesAsync(emvPayload, options, ct);
    }

    public Task RenderToStreamAsync(string emvPayload, Stream outputStream, QrRenderOptions options, CancellationToken ct = default)
    {
        options.Template = "print";
        return _service.RenderToStreamAsync(emvPayload, outputStream, options, ct);
    }
}
