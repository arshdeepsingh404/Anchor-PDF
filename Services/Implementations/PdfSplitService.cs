using System.IO;
using Anchor_PDF.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Anchor_PDF.Services.Implementations;

public class PdfSplitService : IPdfSplitService
{
    #region Public Methods

    /// <summary>
    /// Gets the total number of pages in a PDF document.
    /// </summary>
    public async Task<int> GetPageCountAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("PDF file not found.", filePath);
            }

            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PdfDocument document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            return document.PageCount;
        }, cancellationToken);
    }

    /// <summary>
    /// Splits every page of the source PDF into its own separate PDF document.
    /// </summary>
    public async Task SplitAllPagesAsync(
        string sourcePdf,
        string outputDirectory,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            EnsureDirectoryExists(outputDirectory);
            string baseName = Path.GetFileNameWithoutExtension(sourcePdf);

            using FileStream sourceStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PdfDocument inputDocument = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
            int totalPages = inputDocument.PageCount;

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(new ConversionProgress(
                    Percentage: (double)pageIndex / totalPages * 100.0,
                    CurrentItem: pageIndex + 1,
                    TotalItems: totalPages,
                    Message: $"Extracting page {pageIndex + 1} of {totalPages}..."
                ));

                string pageOutputPath = Path.Combine(outputDirectory, $"{baseName}_page_{pageIndex + 1:D3}.pdf");
                using PdfDocument singlePageDoc = new PdfDocument();
                singlePageDoc.AddPage(inputDocument.Pages[pageIndex]);
                SaveDocumentDirect(singlePageDoc, pageOutputPath);
            }

            progress?.Report(new ConversionProgress(
                Percentage: 100.0,
                CurrentItem: totalPages,
                TotalItems: totalPages,
                Message: $"Successfully split into {totalPages} separate PDF files!"
            ));
        }, cancellationToken);
    }

    /// <summary>
    /// Splits the PDF by range expression (e.g. "1-3, 5, 8-10").
    /// </summary>
    public async Task SplitByRangeAsync(
        string sourcePdf,
        string outputDirectory,
        string rangeExpression,
        bool combineIntoSingleFile,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            EnsureDirectoryExists(outputDirectory);
            string baseName = Path.GetFileNameWithoutExtension(sourcePdf);

            using FileStream sourceStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PdfDocument inputDocument = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
            int totalPages = inputDocument.PageCount;

            List<List<int>> rangeGroups = ParseRangeGroups(rangeExpression, totalPages);
            if (rangeGroups.Count == 0)
            {
                throw new ArgumentException("No valid page numbers found in the specified range expression.", nameof(rangeExpression));
            }

            if (combineIntoSingleFile)
            {
                using PdfDocument combinedDoc = new PdfDocument();
                List<int> allPages = rangeGroups.SelectMany(g => g).Distinct().OrderBy(p => p).ToList();

                for (int i = 0; i < allPages.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int pageNumber = allPages[i];
                    progress?.Report(new ConversionProgress(
                        Percentage: (double)i / allPages.Count * 90.0,
                        CurrentItem: i + 1,
                        TotalItems: allPages.Count,
                        Message: $"Adding page {pageNumber} ({i + 1} of {allPages.Count})..."
                    ));

                    combinedDoc.AddPage(inputDocument.Pages[pageNumber - 1]);
                }

                string outputPath = Path.Combine(outputDirectory, $"{baseName}_extracted_pages.pdf");
                SaveDocumentDirect(combinedDoc, outputPath);

                progress?.Report(new ConversionProgress(
                    Percentage: 100.0,
                    CurrentItem: allPages.Count,
                    TotalItems: allPages.Count,
                    Message: $"Extracted {allPages.Count} pages into '{Path.GetFileName(outputPath)}'!"
                ));
            }
            else
            {
                int totalGroups = rangeGroups.Count;
                for (int groupIdx = 0; groupIdx < totalGroups; groupIdx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    List<int> groupPages = rangeGroups[groupIdx];
                    if (groupPages.Count == 0)
                    {
                        continue;
                    }

                    progress?.Report(new ConversionProgress(
                        Percentage: (double)groupIdx / totalGroups * 100.0,
                        CurrentItem: groupIdx + 1,
                        TotalItems: totalGroups,
                        Message: $"Generating range file {groupIdx + 1} of {totalGroups}..."
                    ));

                    string rangeLabel = groupPages.Count == 1 
                        ? $"page_{groupPages[0]}" 
                        : $"pages_{groupPages[0]}-{groupPages[^1]}";

                    string outputPath = Path.Combine(outputDirectory, $"{baseName}_{rangeLabel}.pdf");
                    using PdfDocument rangeDoc = new PdfDocument();

                    foreach (int pNum in groupPages)
                    {
                        rangeDoc.AddPage(inputDocument.Pages[pNum - 1]);
                    }

                    SaveDocumentDirect(rangeDoc, outputPath);
                }

                progress?.Report(new ConversionProgress(
                    Percentage: 100.0,
                    CurrentItem: totalGroups,
                    TotalItems: totalGroups,
                    Message: $"Successfully extracted {totalGroups} range document(s)!"
                ));
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Splits the PDF into chunks of N pages each.
    /// </summary>
    public async Task SplitEveryNPagesAsync(
        string sourcePdf,
        string outputDirectory,
        int pagesPerChunk,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (pagesPerChunk <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pagesPerChunk), "Pages per chunk must be greater than zero.");
        }

        await Task.Run(() =>
        {
            EnsureDirectoryExists(outputDirectory);
            string baseName = Path.GetFileNameWithoutExtension(sourcePdf);

            using FileStream sourceStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PdfDocument inputDocument = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);
            int totalPages = inputDocument.PageCount;
            int chunkCount = (int)Math.Ceiling((double)totalPages / pagesPerChunk);

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int startPage = chunkIndex * pagesPerChunk;
                int endPage = Math.Min(startPage + pagesPerChunk, totalPages);

                progress?.Report(new ConversionProgress(
                    Percentage: (double)chunkIndex / chunkCount * 100.0,
                    CurrentItem: chunkIndex + 1,
                    TotalItems: chunkCount,
                    Message: $"Creating part {chunkIndex + 1} of {chunkCount} (pages {startPage + 1} to {endPage})..."
                ));

                using PdfDocument chunkDoc = new PdfDocument();
                for (int pageIdx = startPage; pageIdx < endPage; pageIdx++)
                {
                    chunkDoc.AddPage(inputDocument.Pages[pageIdx]);
                }

                string outputPath = Path.Combine(outputDirectory, $"{baseName}_part_{chunkIndex + 1:D2}.pdf");
                SaveDocumentDirect(chunkDoc, outputPath);
            }

            progress?.Report(new ConversionProgress(
                Percentage: 100.0,
                CurrentItem: chunkCount,
                TotalItems: chunkCount,
                Message: $"Successfully split into {chunkCount} chunk document(s)!"
            ));
        }, cancellationToken);
    }

    #endregion

    #region Private Helper Methods

    private static void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void SaveDocumentDirect(PdfDocument document, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            FileInfo fileInfo = new FileInfo(targetPath);
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }
        }

        using FileStream outputStream = new FileStream(
            targetPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            65536,
            useAsync: true);

        document.Save(outputStream);
    }

    private static List<List<int>> ParseRangeGroups(string rangeExpression, int totalPages)
    {
        List<List<int>> groups = [];
        if (string.IsNullOrWhiteSpace(rangeExpression))
        {
            return groups;
        }

        string[] parts = rangeExpression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            List<int> groupPages = [];
            if (part.Contains('-'))
            {
                string[] subParts = part.Split('-');
                if (subParts.Length == 2 &&
                    int.TryParse(subParts[0].Trim(), out int start) &&
                    int.TryParse(subParts[1].Trim(), out int end))
                {
                    int min = Math.Max(1, Math.Min(start, end));
                    int max = Math.Min(totalPages, Math.Max(start, end));

                    for (int p = min; p <= max; p++)
                    {
                        groupPages.Add(p);
                    }
                }
            }
            else if (int.TryParse(part, out int singlePage))
            {
                if (singlePage >= 1 && singlePage <= totalPages)
                {
                    groupPages.Add(singlePage);
                }
            }

            if (groupPages.Count > 0)
            {
                groups.Add(groupPages);
            }
        }

        return groups;
    }

    #endregion
}
