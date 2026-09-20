using Anchor_PDF.Models;

namespace Anchor_PDF.Services;

public interface IImageToPdfService
{
    Task ConvertImagesToPdfAsync(
        IReadOnlyList<ImageItem> images,
        ImageToPdfOptions options,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default
    );

    Task<ImageItem> LoadImageItemAsync(string filePath, int orderIndex, CancellationToken cancellationToken = default);
}
