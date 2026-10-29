namespace VietQR.Core;

/// <summary>Render ảnh QR. Implementation nằm ở Rendering.</summary>
public interface IQrImageRenderer
{
    Task<byte[]> RenderPngAsync(
        string emvPayload,
        QrRenderOptions options,
        CancellationToken ct = default);

    Task<byte[]> RenderAsync(
        string emvPayload,
        QrRenderOptions options,
        CancellationToken ct = default);

    Task RenderToStreamAsync(
        string emvPayload,
        Stream outputStream,
        QrRenderOptions options,
        CancellationToken ct = default);
}
