using Anchor_PDF.Models;

namespace Anchor_PDF.Services;

public interface IPdfCompressService
{
    /// <summary>
    /// Compresses a PDF document according to the specified preset.
    /// </summary>
    Task<CompressionResult> CompressPdfAsync(
        string sourcePdf,
        string outputPdfPath,
        CompressionPreset preset,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
