namespace VietQR.Core;

public sealed class QrOnlyTemplate : IQrTemplate
{
    private readonly QrImageService _service;

    public QrOnlyTemplate(QrImageService service)
    {
        _service = service;
    }

    public string Name => "qr_only";

    public Task<byte[]> RenderAsync(string emvPayload, QrRenderOptions options, CancellationToken ct = default)
    {
        options.Template = "qr_only";
        return _service.RenderBytesAsync(emvPayload, options, ct);
    }

    public Task RenderToStreamAsync(string emvPayload, Stream outputStream, QrRenderOptions options, CancellationToken ct = default)
    {
        options.Template = "qr_only";
        return _service.RenderToStreamAsync(emvPayload, outputStream, options, ct);
    }
}
