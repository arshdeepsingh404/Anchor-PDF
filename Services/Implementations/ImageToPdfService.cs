using System.IO;
using System.Windows.Media.Imaging;
using Anchor_PDF.Models;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Anchor_PDF.Services.Implementations;

public class ImageToPdfService : IImageToPdfService
{
    #region Public Methods

    /// <summary>
    /// Loads an image item, extracting dimensions and generating a lightweight frozen thumbnail.
    /// </summary>
    public async Task<ImageItem> LoadImageItemAsync(string filePath, int orderIndex, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Image file not found.", filePath);
            }

            byte[] imageBytes = File.ReadAllBytes(filePath);
            int fullWidth = 0;
            int fullHeight = 0;

            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                if (decoder.Frames.Count > 0)
                {
                    fullWidth = decoder.Frames[0].PixelWidth;
                    fullHeight = decoder.Frames[0].PixelHeight;
                }
            }

            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.DecodePixelWidth = 220; // Lightweight thumbnail for UI performance
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.EndInit();
            }
            bitmap.Freeze(); // Freezing allows UI binding across threads

            return new ImageItem
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                OrderIndex = orderIndex,
                PixelWidth = fullWidth,
                PixelHeight = fullHeight,
                Thumbnail = bitmap,
                IsSelected = true
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Compiles the provided images into a structured PDF document with layout and margin presets.
    /// </summary>
    public async Task ConvertImagesToPdfAsync(
        IReadOnlyList<ImageItem> images,
        ImageToPdfOptions options,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using PdfDocument document = new PdfDocument();
            int total = images.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ImageItem item = images[i];
                progress?.Report(new ConversionProgress(
                    Percentage: (double)i / total * 100.0,
                    CurrentItem: i + 1,
                    TotalItems: total,
                    Message: $"Embedding {item.FileName} ({i + 1} of {total})..."
                ));

                if (!File.Exists(item.FilePath))
                {
                    continue;
                }

                using XImage xImage = XImage.FromFile(item.FilePath);
                PdfPage page = document.AddPage();

                if (options.PageSize == PageSizePreset.FitToImage)
                {
                    // Fit page exactly to the image's physical point dimensions
                    page.Width = XUnit.FromPoint(xImage.PointWidth);
                    page.Height = XUnit.FromPoint(xImage.PointHeight);

                    using XGraphics gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
                }
                else
                {
                    // Fixed presets: A4 or Letter
                    page.Size = options.PageSize switch
                    {
                        PageSizePreset.A4 => PageSize.A4,
                        PageSizePreset.Letter => PageSize.Letter,
                        _ => PageSize.A4
                    };

                    // Handle Orientation
                    if (options.Orientation == PageOrientationPreset.Auto)
                    {
                        page.Orientation = xImage.PixelWidth > xImage.PixelHeight 
                            ? PageOrientation.Landscape 
                            : PageOrientation.Portrait;
                    }
                    else if (options.Orientation == PageOrientationPreset.Landscape)
                    {
                        page.Orientation = PageOrientation.Landscape;
                    }
                    else
                    {
                        page.Orientation = PageOrientation.Portrait;
                    }

                    double margin = Math.Max(0, options.MarginPoints);
                    double availW = Math.Max(10, page.Width.Point - 2 * margin);
                    double availH = Math.Max(10, page.Height.Point - 2 * margin);

                    // Aspect-ratio fit inside margins
                    double scale = Math.Min(availW / xImage.PointWidth, availH / xImage.PointHeight);
                    double drawW = xImage.PointWidth * scale;
                    double drawH = xImage.PointHeight * scale;

                    double drawX = margin + (availW - drawW) / 2.0;
                    double drawY = margin + (availH - drawH) / 2.0;

                    using XGraphics gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawImage(xImage, drawX, drawY, drawW, drawH);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ConversionProgress(
                Percentage: 98.0,
                CurrentItem: total,
                TotalItems: total,
                Message: "Saving compiled PDF document..."
            ));

            string? targetDirectory = Path.GetDirectoryName(options.OutputPdfPath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (File.Exists(options.OutputPdfPath))
            {
                FileInfo existingFileInfo = new FileInfo(options.OutputPdfPath);
                if (existingFileInfo.IsReadOnly)
                {
                    existingFileInfo.IsReadOnly = false;
                }
            }

            try
            {
                using FileStream outputStream = new FileStream(
                    options.OutputPdfPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    65536,
                    useAsync: true);

                document.Save(outputStream);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new IOException(
                    $"Cannot save '{Path.GetFileName(options.OutputPdfPath)}' because it is currently open or locked by another process (such as Adobe Acrobat, Microsoft Edge, or Google Chrome).\n\nPlease close the PDF viewer and try again, or save with a different file name.", exception);
            }

            progress?.Report(new ConversionProgress(
                Percentage: 100.0,
                CurrentItem: total,
                TotalItems: total,
                Message: "PDF created successfully!"
            ));
        }, cancellationToken);
    }

    #endregion
}
