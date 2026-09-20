using System.Windows.Media.Imaging;
using Anchor_PDF.Models;

namespace Anchor_PDF.Services;

public interface IPdfToImageService
{
    Task<int> GetPageCountAsync(string pdfPath, CancellationToken cancellationToken = default);

    Task<BitmapSource?> RenderPageThumbnailAsync(
        string pdfPath,
        int pageNumber,
        int dpi = 50,
        CancellationToken cancellationToken = default
    );

    Task ConvertPdfToImagesAsync(
        PdfToImageOptions options,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default
    );

    List<int> ParsePageRange(string pageRangeString, int totalPages);
}
