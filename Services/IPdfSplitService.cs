using Anchor_PDF.Models;

namespace Anchor_PDF.Services;

public interface IPdfSplitService
{
    /// <summary>
    /// Gets the total number of pages in a PDF document.
    /// </summary>
    Task<int> GetPageCountAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits every page of the source PDF into its own separate PDF document.
    /// </summary>
    Task SplitAllPagesAsync(
        string sourcePdf,
        string outputDirectory,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits the PDF by range expression (e.g. "1-3, 5, 8-10").
    /// If combineIntoSingleFile is true, all matching pages are saved to one PDF; otherwise each part is saved separately.
    /// </summary>
    Task SplitByRangeAsync(
        string sourcePdf,
        string outputDirectory,
        string rangeExpression,
        bool combineIntoSingleFile,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits the PDF into chunks of N pages each.
    /// </summary>
    Task SplitEveryNPagesAsync(
        string sourcePdf,
        string outputDirectory,
        int pagesPerChunk,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
