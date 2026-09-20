using Anchor_PDF.Models;

namespace Anchor_PDF.Services;

public interface IPdfReorganizeService
{
    /// <summary>
    /// Gets the total number of pages in a PDF document.
    /// </summary>
    Task<int> GetPageCountAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles a new PDF with pages reordered, rotated, duplicated, or removed according to the provided page list.
    /// </summary>
    Task ReorganizePdfAsync(
        string sourcePdf,
        IReadOnlyList<ReorganizePageItem> pages,
        string outputPdfPath,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
