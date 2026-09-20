using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anchor_PDF.Models;

public partial class PdfPageItem : ObservableObject
{
    public int PageNumber { get; init; }

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private BitmapSource? _thumbnail;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private int _pixelWidth;

    [ObservableProperty]
    private int _pixelHeight;

    public string PageLabel => $"Page {PageNumber}";

    public string DimensionText => PixelWidth > 0 && PixelHeight > 0
        ? $"{PixelWidth} × {PixelHeight} px"
        : string.Empty;
}
