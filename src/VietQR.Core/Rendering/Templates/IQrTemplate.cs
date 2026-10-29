namespace VietQR.Core;

public interface IQrTemplate
{
    string Name { get; }
    Task<byte[]> RenderAsync(string emvPayload, QrRenderOptions options, CancellationToken ct = default);
    Task RenderToStreamAsync(string emvPayload, Stream outputStream, QrRenderOptions options, CancellationToken ct = default);
}
