namespace VietQR.Core;

/// <summary>Render ảnh PNG. Implementation nằm ở Rendering.</summary>
public interface IQrImageRenderer
{
    Task<byte[]> RenderPngAsync(
        string emvPayload,
        QrRenderOptions options,
        CancellationToken ct = default);
}
