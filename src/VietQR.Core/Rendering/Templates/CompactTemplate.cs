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
        options.Template = "compact";
        return _service.RenderBytesAsync(emvPayload, options, ct);
    }

    public Task RenderToStreamAsync(string emvPayload, Stream outputStream, QrRenderOptions options, CancellationToken ct = default)
    {
        options.Template = "compact";
        return _service.RenderToStreamAsync(emvPayload, outputStream, options, ct);
    }
}
