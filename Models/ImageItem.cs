using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anchor_PDF.Models;

public partial class ImageItem : ObservableObject
{
    public required string FilePath { get; init; }
    public string FileName => Path.GetFileName(FilePath);
    public long FileSizeBytes { get; init; }

    [ObservableProperty]
    private int _orderIndex;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private int _pixelWidth;

    [ObservableProperty]
    private int _pixelHeight;

    [ObservableProperty]
    private BitmapSource? _thumbnail;

    public string FormattedFileSize
    {
        get
        {
            if (FileSizeBytes < 1024)
                return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
        }
    }

    public string ResolutionText => PixelWidth > 0 && PixelHeight > 0 
        ? $"{PixelWidth} × {PixelHeight} px" 
        : string.Empty;
}
