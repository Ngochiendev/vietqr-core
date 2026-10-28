namespace VietQR.Core;

public interface IQrTemplate
{
    string Name { get; }
    Task<byte[]> RenderAsync(string emvPayload, QrRenderOptions options, CancellationToken ct = default);
}
