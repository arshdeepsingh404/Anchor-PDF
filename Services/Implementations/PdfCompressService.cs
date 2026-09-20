using System.IO;
using Anchor_PDF.Models;
using PDFtoImage;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Anchor_PDF.Services.Implementations;

public class PdfCompressService : IPdfCompressService
{
    #region Public Methods

    /// <summary>
    /// Compresses a PDF document according to the specified preset.
    /// </summary>
    public async Task<CompressionResult> CompressPdfAsync(
        string sourcePdf,
        string outputPdfPath,
        CompressionPreset preset,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePdf))
        {
            throw new FileNotFoundException("Source PDF file not found.", sourcePdf);
        }

        FileInfo sourceInfo = new FileInfo(sourcePdf);
        long originalBytes = sourceInfo.Length;

        string? targetDirectory = Path.GetDirectoryName(outputPdfPath);
        if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        if (File.Exists(outputPdfPath))
        {
            FileInfo targetInfo = new FileInfo(outputPdfPath);
            if (targetInfo.IsReadOnly)
            {
                targetInfo.IsReadOnly = false;
            }
        }

        if (preset == CompressionPreset.LosslessStreamOptimization)
        {
            await CompressStreamLosslessAsync(sourcePdf, outputPdfPath, progress, cancellationToken);
        }
        else
        {
            int dpi = preset == CompressionPreset.MaximumCompression ? 96 : 150;
            await CompressRasterAsync(sourcePdf, outputPdfPath, dpi, progress, cancellationToken);
        }

        FileInfo resultInfo = new FileInfo(outputPdfPath);
        long compressedBytes = resultInfo.Length;

        progress?.Report(new ConversionProgress(
            Percentage: 100.0,
            CurrentItem: 1,
            TotalItems: 1,
            Message: "Compression finished successfully!"
        ));

        return new CompressionResult
        {
            OriginalSizeBytes = originalBytes,
            CompressedSizeBytes = compressedBytes
        };
    }

    #endregion

    #region Private Helper Methods

    private static async Task CompressStreamLosslessAsync(
        string sourcePdf,
        string outputPdfPath,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using PdfDocument outputDocument = new PdfDocument();
            outputDocument.Options.CompressContentStreams = true;

            using (FileStream sourceStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (PdfDocument inputDocument = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Import))
            {
                int totalPages = inputDocument.PageCount;
                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(new ConversionProgress(
                        Percentage: (double)i / totalPages * 90.0,
                        CurrentItem: i + 1,
                        TotalItems: totalPages,
                        Message: $"Optimizing streams for page {i + 1} of {totalPages}..."
                    ));

                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ConversionProgress(
                Percentage: 95.0,
                CurrentItem: 1,
                TotalItems: 1,
                Message: "Writing optimized PDF document..."
            ));

            using FileStream outputStream = new FileStream(
                outputPdfPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                65536,
                useAsync: true);

            outputDocument.Save(outputStream);
        }, cancellationToken);
    }

    private static async Task CompressRasterAsync(
        string sourcePdf,
        string outputPdfPath,
        int dpi,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            int totalPages = 0;
            using (FileStream probeStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                totalPages = Conversion.GetPageCount(probeStream);
            }

            RenderOptions renderOptions = new RenderOptions(Dpi: dpi);
            using PdfDocument outputDocument = new PdfDocument();
            outputDocument.Options.CompressContentStreams = true;

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new ConversionProgress(
                    Percentage: (double)pageIndex / totalPages * 90.0,
                    CurrentItem: pageIndex + 1,
                    TotalItems: totalPages,
                    Message: $"Compressing page {pageIndex + 1} of {totalPages} ({dpi} DPI)..."
                ));

                byte[] jpegBytes;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (FileStream pdfStream = new FileStream(sourcePdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Conversion.SaveJpeg(memoryStream, pdfStream, page: pageIndex, options: renderOptions);
                    }
                    jpegBytes = memoryStream.ToArray();
                }

                using MemoryStream imgStream = new MemoryStream(jpegBytes);
                using XImage xImage = XImage.FromStream(imgStream);
                PdfPage page = outputDocument.AddPage();
                page.Width = XUnit.FromPoint(xImage.PointWidth);
                page.Height = XUnit.FromPoint(xImage.PointHeight);

                using XGraphics gfx = XGraphics.FromPdfPage(page);
                gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new ConversionProgress(
                Percentage: 95.0,
                CurrentItem: totalPages,
                TotalItems: totalPages,
                Message: "Finalizing compressed PDF document..."
            ));

            using FileStream outputStream = new FileStream(
                outputPdfPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                65536,
                useAsync: true);

            outputDocument.Save(outputStream);
        }, cancellationToken);
    }

    #endregion
}
