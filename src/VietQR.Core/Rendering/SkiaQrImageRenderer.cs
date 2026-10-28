using Microsoft.Extensions.Logging;

namespace VietQR.Core;

public sealed class SkiaQrImageRenderer : IQrImageRenderer
{
    private readonly IReadOnlyDictionary<string, IQrTemplate> _templates;
    private readonly ILogger<SkiaQrImageRenderer>? _logger;

    public SkiaQrImageRenderer(
        IEnumerable<IQrTemplate> templates,
        ILogger<SkiaQrImageRenderer>? logger = null)
    {
        _templates = templates.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<byte[]> RenderPngAsync(
        string emvPayload,
        QrRenderOptions options,
        CancellationToken ct = default)
    {
        var templateName = string.IsNullOrWhiteSpace(options.Template) ? "compact" : options.Template;

        if (!_templates.TryGetValue(templateName, out var template))
        {
            throw new ArgumentException($"Template '{options.Template}' không hỗ trợ.", nameof(options));
        }

        _logger?.LogDebug("Rendering QR with template {Template}", templateName);
        return await template.RenderAsync(emvPayload, options, ct);
    }
}
