using System.IO;
using System.IO.Compression;
using System.Windows.Media.Imaging;
using Anchor_PDF.Models;
using PDFtoImage;

namespace Anchor_PDF.Services.Implementations;

public class PdfToImageService : IPdfToImageService
{
    #region Fields

    /// <summary>
    /// Global lock to serialize native PDFium calls, preventing multi-threaded Access Violation crashes.
    /// </summary>
    private static readonly SemaphoreSlim _pdfiumLock = new SemaphoreSlim(1, 1);

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads total page count from the specified PDF file.
    /// </summary>
    public async Task<int> GetPageCountAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("PDF file not found.", pdfPath);
        }

        await _pdfiumLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using FileStream stream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return Conversion.GetPageCount(stream);
            }, cancellationToken);
        }
        finally
        {
            _pdfiumLock.Release();
        }
    }

    /// <summary>
    /// Renders a fast, high-quality thumbnail of a single PDF page for UI previews.
    /// </summary>
    public async Task<BitmapSource?> RenderPageThumbnailAsync(
        string pdfPath,
        int pageNumber,
        int dpi = 50,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pdfPath))
        {
            return null;
        }

        await _pdfiumLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using FileStream pdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using MemoryStream memoryStream = new MemoryStream();
                    RenderOptions renderOptions = new RenderOptions(Dpi: dpi);

                    Conversion.SavePng(memoryStream, pdfStream, page: pageNumber - 1, options: renderOptions);

                    memoryStream.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return (BitmapSource)bitmapImage;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch
                {
                    return null;
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            _pdfiumLock.Release();
        }
    }

    /// <summary>
    /// Parses comma-separated and dash-separated page range strings into distinct 1-based page numbers.
    /// </summary>
    public List<int> ParsePageRange(string pageRangeString, int totalPages)
    {
        if (string.IsNullOrWhiteSpace(pageRangeString) || 
            pageRangeString.Trim().Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return Enumerable.Range(1, totalPages).ToList();
        }

        SortedSet<int> result = new SortedSet<int>();
        string[] parts = pageRangeString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string part in parts)
        {
            if (part.Contains('-'))
            {
                string[] rangeParts = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (rangeParts.Length == 2 && 
                    int.TryParse(rangeParts[0], out int start) && 
                    int.TryParse(rangeParts[1], out int end))
                {
                    int min = Math.Max(1, Math.Min(start, end));
                    int max = Math.Min(totalPages, Math.Max(start, end));
                    for (int i = min; i <= max; i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else if (int.TryParse(part, out int singlePage))
            {
                if (singlePage >= 1 && singlePage <= totalPages)
                {
                    result.Add(singlePage);
                }
            }
        }

        return result.Count > 0 ? result.ToList() : Enumerable.Range(1, totalPages).ToList();
    }

    /// <summary>
    /// Converts the specified PDF pages to image files or packages them directly into a ZIP archive.
    /// </summary>
    public async Task ConvertPdfToImagesAsync(
        PdfToImageOptions options,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(options.SourcePdfPath))
        {
            throw new FileNotFoundException("PDF file not found.", options.SourcePdfPath);
        }

        int totalPages = await GetPageCountAsync(options.SourcePdfPath, cancellationToken);

        List<int> pagesToConvert;
        if (options.SpecificPages != null && options.SpecificPages.Count > 0)
        {
            pagesToConvert = options.SpecificPages
                .Where(p => p >= 1 && p <= totalPages)
                .Distinct()
                .OrderBy(p => p)
                .ToList();
        }
        else
        {
            pagesToConvert = ParsePageRange(options.PageRange, totalPages);
        }

        if (pagesToConvert.Count == 0)
        {
            throw new InvalidOperationException("No valid pages selected to convert.");
        }

        string pdfFileName = Path.GetFileNameWithoutExtension(options.SourcePdfPath);
        string extension = options.Format switch
        {
            ImageFormatType.PNG => ".png",
            ImageFormatType.JPEG => ".jpg",
            ImageFormatType.WEBP => ".webp",
            _ => ".png"
        };

        RenderOptions renderOptions = new RenderOptions(Dpi: options.Dpi);

        if (options.ExportAsZip)
        {
            string defaultZipName = $"{pdfFileName}_images.zip";
            string targetZipPath = options.ZipFilePath ?? Path.Combine(options.OutputDirectory, defaultZipName);

            string? zipDirectory = Path.GetDirectoryName(targetZipPath);
            if (!string.IsNullOrEmpty(zipDirectory) && !Directory.Exists(zipDirectory))
            {
                Directory.CreateDirectory(zipDirectory);
            }

            if (File.Exists(targetZipPath))
            {
                FileInfo existingZipInfo = new FileInfo(targetZipPath);
                if (existingZipInfo.IsReadOnly)
                {
                    existingZipInfo.IsReadOnly = false;
                }
            }

            using (FileStream zipFileStream = new FileStream(targetZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
            using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
            {
                for (int i = 0; i < pagesToConvert.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int pageNum = pagesToConvert[i];
                    int pageIndex = pageNum - 1;
                    string entryName = $"{pdfFileName}_page_{pageNum:D3}{extension}";

                    progress?.Report(new ConversionProgress(
                        Percentage: (double)i / pagesToConvert.Count * 95.0,
                        CurrentItem: i + 1,
                        TotalItems: pagesToConvert.Count,
                        Message: $"Rendering page {pageNum} of {totalPages} into ZIP..."
                    ));

                    ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                    await _pdfiumLock.WaitAsync(cancellationToken);
                    try
                    {
                        await Task.Run(() =>
                        {
                            using MemoryStream memoryStream = new MemoryStream();
                            using FileStream pdfStream = new FileStream(options.SourcePdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                            switch (options.Format)
                            {
                                case ImageFormatType.PNG:
                                    Conversion.SavePng(memoryStream, pdfStream, page: pageIndex, options: renderOptions);
                                    break;
                                case ImageFormatType.JPEG:
                                    Conversion.SaveJpeg(memoryStream, pdfStream, page: pageIndex, options: renderOptions);
                                    break;
                                case ImageFormatType.WEBP:
                                    Conversion.SaveWebp(memoryStream, pdfStream, page: pageIndex, options: renderOptions);
                                    break;
                            }

                            memoryStream.Position = 0;
                            using Stream entryStream = entry.Open();
                            memoryStream.CopyTo(entryStream);
                        }, cancellationToken);
                    }
                    finally
                    {
                        _pdfiumLock.Release();
                    }
                }
            }

            options.ZipFilePath = targetZipPath;
            options.GeneratedFiles.Add(targetZipPath);
        }
        else
        {
            for (int i = 0; i < pagesToConvert.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int pageNum = pagesToConvert[i];
                int pageIndex = pageNum - 1;
                string outFileName = $"{pdfFileName}_page_{pageNum:D3}{extension}";
                string targetFilePath = Path.Combine(options.OutputDirectory, outFileName);

                progress?.Report(new ConversionProgress(
                    Percentage: (double)i / pagesToConvert.Count * 95.0,
                    CurrentItem: i + 1,
                    TotalItems: pagesToConvert.Count,
                    Message: $"Rendering page {pageNum} of {totalPages}..."
                ));

                string? targetDirectory = Path.GetDirectoryName(targetFilePath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (File.Exists(targetFilePath))
                {
                    FileInfo existingFileInfo = new FileInfo(targetFilePath);
                    if (existingFileInfo.IsReadOnly)
                    {
                        existingFileInfo.IsReadOnly = false;
                    }
                }

                await _pdfiumLock.WaitAsync(cancellationToken);
                try
                {
                    await Task.Run(() =>
                    {
                        using FileStream outputStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
                        using FileStream pdfStream = new FileStream(options.SourcePdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        switch (options.Format)
                        {
                            case ImageFormatType.PNG:
                                Conversion.SavePng(outputStream, pdfStream, page: pageIndex, options: renderOptions);
                                break;
                            case ImageFormatType.JPEG:
                                Conversion.SaveJpeg(outputStream, pdfStream, page: pageIndex, options: renderOptions);
                                break;
                            case ImageFormatType.WEBP:
                                Conversion.SaveWebp(outputStream, pdfStream, page: pageIndex, options: renderOptions);
                                break;
                        }
                    }, cancellationToken);
                }
                finally
                {
                    _pdfiumLock.Release();
                }

                options.GeneratedFiles.Add(targetFilePath);
            }
        }

        progress?.Report(new ConversionProgress(
            Percentage: 100.0,
            CurrentItem: pagesToConvert.Count,
            TotalItems: pagesToConvert.Count,
            Message: "Conversion completed successfully!"
        ));
    }

    #endregion
}
