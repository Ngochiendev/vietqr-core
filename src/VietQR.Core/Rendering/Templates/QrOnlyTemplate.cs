namespace VietQR.Core.Rendering.Templates;

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
        var bytes = _service.GenerateQrOnlyBytes(emvPayload, options.Size > 0 ? options.Size : 500);
        return Task.FromResult(bytes);
    }
}
