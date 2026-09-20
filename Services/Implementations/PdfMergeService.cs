using System.IO;
using Anchor_PDF.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Anchor_PDF.Services.Implementations;

public class PdfMergeService : IPdfMergeService
{
    #region Public Methods

    /// <summary>
    /// Reads PDF metadata and page count for a given file path.
    /// </summary>
    public async Task<MergePdfItem> LoadPdfItemAsync(string filePath, int orderIndex, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("PDF file not found.", filePath);
            }

            int pageCount = 0;
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (PdfDocument document = PdfReader.Open(stream, PdfDocumentOpenMode.Import))
            {
                pageCount = document.PageCount;
            }

            return new MergePdfItem
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                OrderIndex = orderIndex,
                PageCount = pageCount,
                IsSelected = true
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Merges the specified PDF files in order into a single destination PDF.
    /// </summary>
    public async Task MergePdfsAsync(
        IReadOnlyList<MergePdfItem> items,
        string outputPdfPath,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using PdfDocument outputDocument = new PdfDocument();
            int totalFiles = items.Count;
            int totalPagesMerged = 0;

            for (int fileIndex = 0; fileIndex < totalFiles; fileIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                MergePdfItem item = items[fileIndex];
                progress?.Report(new ConversionProgress(
                    Percentage: (double)fileIndex / totalFiles * 90.0,
                    CurrentItem: fileIndex + 1,
                    TotalItems: totalFiles,
                    Message: $"Merging {item.FileName} ({fileIndex + 1} of {totalFiles})..."
                ));

                if (!File.Exists(item.FilePath))
                {
                    continue;
                }

                using (FileStream stream = new FileStream(item.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (PdfDocument inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import))
                {
                    int pageCount = inputDocument.PageCount;
                    for (int pageIdx = 0; pageIdx < pageCount; pageIdx++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        outputDocument.AddPage(inputDocument.Pages[pageIdx]);
                        totalPagesMerged++;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ConversionProgress(
                Percentage: 95.0,
                CurrentItem: totalFiles,
                TotalItems: totalFiles,
                Message: "Saving merged PDF document..."
            ));

            string? targetDirectory = Path.GetDirectoryName(outputPdfPath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (File.Exists(outputPdfPath))
            {
                FileInfo existingFileInfo = new FileInfo(outputPdfPath);
                if (existingFileInfo.IsReadOnly)
                {
                    existingFileInfo.IsReadOnly = false;
                }
            }

            try
            {
                using FileStream outputStream = new FileStream(
                    outputPdfPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    65536,
                    useAsync: true);

                outputDocument.Save(outputStream);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new IOException(
                    $"Cannot save '{Path.GetFileName(outputPdfPath)}' because it is currently open or locked by another process.\n\nPlease close the PDF viewer and try again.", exception);
            }

            progress?.Report(new ConversionProgress(
                Percentage: 100.0,
                CurrentItem: totalFiles,
                TotalItems: totalFiles,
                Message: $"Successfully merged {totalFiles} files ({totalPagesMerged} pages)!"
            ));
        }, cancellationToken);
    }

    #endregion
}
