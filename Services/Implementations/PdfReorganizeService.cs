using System.IO;
using Anchor_PDF.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Anchor_PDF.Services.Implementations;

public class PdfReorganizeService : IPdfReorganizeService
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
    /// Compiles a new PDF with pages reordered, rotated, duplicated, or removed according to the provided page list.
    /// </summary>
    public async Task ReorganizePdfAsync(
        string sourcePdf,
        IReadOnlyList<ReorganizePageItem> pages,
        string outputPdfPath,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(sourcePdf))
            {
                throw new FileNotFoundException("Source PDF not found.", sourcePdf);
            }

            using PdfDocument outputDocument = new PdfDocument();
            using FileStream sourceStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using PdfDocument inputDocument = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import);

            int totalPages = pages.Count;
            for (int i = 0; i < totalPages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReorganizePageItem item = pages[i];

                progress?.Report(new ConversionProgress(
                    Percentage: (double)i / totalPages * 90.0,
                    CurrentItem: i + 1,
                    TotalItems: totalPages,
                    Message: $"Processing page {i + 1} of {totalPages} (Original #{item.OriginalPageNumber})..."
                ));

                if (item.OriginalPageNumber >= 1 && item.OriginalPageNumber <= inputDocument.PageCount)
                {
                    PdfPage originalPage = inputDocument.Pages[item.OriginalPageNumber - 1];
                    PdfPage newPage = outputDocument.AddPage(originalPage);

                    int combinedRotation = (newPage.Rotate + item.RotationAngle) % 360;
                    if (combinedRotation < 0)
                    {
                        combinedRotation += 360;
                    }
                    newPage.Rotate = combinedRotation;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ConversionProgress(
                Percentage: 95.0,
                CurrentItem: totalPages,
                TotalItems: totalPages,
                Message: "Saving reorganized PDF..."
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
                CurrentItem: totalPages,
                TotalItems: totalPages,
                Message: $"Successfully saved {totalPages} pages to '{Path.GetFileName(outputPdfPath)}'!"
            ));
        }, cancellationToken);
    }

    #endregion
}
