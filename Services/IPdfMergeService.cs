using Anchor_PDF.Models;

namespace Anchor_PDF.Services;

public interface IPdfMergeService
{
    /// <summary>
    /// Reads PDF metadata and page count for a given file path.
    /// </summary>
    Task<MergePdfItem> LoadPdfItemAsync(string filePath, int orderIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges the specified PDF files in order into a single destination PDF.
    /// </summary>
    Task MergePdfsAsync(
        IReadOnlyList<MergePdfItem> items,
        string outputPdfPath,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
