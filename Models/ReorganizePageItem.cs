using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anchor_PDF.Models;

public partial class ReorganizePageItem : ObservableObject
{
    #region Properties

    public required int OriginalPageNumber { get; init; }

    [ObservableProperty]
    private int _currentOrderIndex;

    [ObservableProperty]
    private int _rotationAngle; // 0, 90, 180, 270

    [ObservableProperty]
    private BitmapSource? _thumbnail;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isLoading = true;

    #endregion
}
