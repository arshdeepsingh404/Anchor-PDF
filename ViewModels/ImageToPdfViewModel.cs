using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Anchor_PDF.Models;
using Anchor_PDF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class ImageToPdfViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IImageToPdfService _imageToPdfService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ImageItem> Images { get; } = [];

    public IReadOnlyList<PageSizePreset> AvailablePageSizes { get; } = 
        [PageSizePreset.FitToImage, PageSizePreset.A4, PageSizePreset.Letter];

    public IReadOnlyList<PageOrientationPreset> AvailableOrientations { get; } = 
        [PageOrientationPreset.Auto, PageOrientationPreset.Portrait, PageOrientationPreset.Landscape];

    public ImageToPdfViewModel(IImageToPdfService imageToPdfService, IDialogService dialogService)
    {
        _imageToPdfService = imageToPdfService;
        _dialogService = dialogService;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private ImageItem? _selectedImage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConvert))]
    private int _selectedImageCount;

    [ObservableProperty]
    private PageSizePreset _selectedPageSize = PageSizePreset.FitToImage;

    [ObservableProperty]
    private PageOrientationPreset _selectedOrientation = PageOrientationPreset.Auto;

    [ObservableProperty]
    private double _marginPoints = 0;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConvert))]
    private bool _isConverting;

    [ObservableProperty]
    private bool _hasImages;

    public bool CanConvert => SelectedImageCount > 0 && !IsConverting;

    #endregion

    #region Relay Commands

    [RelayCommand]
    private async Task AddImagesAsync()
    {
        string filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files (*.*)|*.*";
        string[]? files = _dialogService.ShowOpenFilesDialog(filter, "Select Image Files");
        if (files != null && files.Length > 0)
        {
            await AddFilesAsync(files);
        }
    }

    public async Task AddFilesAsync(IEnumerable<string> filePaths)
    {
        HashSet<string> supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp", ".bmp"
        };

        StatusMessage = "Loading images...";
        int addedCount = 0;

        foreach (string path in filePaths)
        {
            if (!File.Exists(path) || !supportedExtensions.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            try
            {
                ImageItem item = await _imageToPdfService.LoadImageItemAsync(path, Images.Count + 1);
                item.PropertyChanged += ImageItem_PropertyChanged;
                Images.Add(item);
                addedCount++;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error Loading Image", $"Could not load '{Path.GetFileName(path)}': {ex.Message}");
            }
        }

        UpdateOrderIndices();
        HasImages = Images.Count > 0;
        UpdateSelectedImageCount();
    }

    [RelayCommand]
    private void SelectAllImages()
    {
        foreach (ImageItem item in Images)
        {
            item.IsSelected = true;
        }

        UpdateSelectedImageCount();
    }

    [RelayCommand]
    private void DeselectAllImages()
    {
        foreach (ImageItem item in Images)
        {
            item.IsSelected = false;
        }

        UpdateSelectedImageCount();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedImage != null)
        {
            SelectedImage.PropertyChanged -= ImageItem_PropertyChanged;
            Images.Remove(SelectedImage);
            UpdateOrderIndices();
            HasImages = Images.Count > 0;
            UpdateSelectedImageCount();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (ImageItem item in Images)
        {
            item.PropertyChanged -= ImageItem_PropertyChanged;
        }

        Images.Clear();
        HasImages = false;
        UpdateSelectedImageCount();
        StatusMessage = "Cleared all images.";
    }

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedImage == null)
        {
            return;
        }

        int index = Images.IndexOf(SelectedImage);
        if (index > 0)
        {
            Images.Move(index, index - 1);
            UpdateOrderIndices();
        }
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedImage == null)
        {
            return;
        }

        int index = Images.IndexOf(SelectedImage);
        if (index >= 0 && index < Images.Count - 1)
        {
            Images.Move(index, index + 1);
            UpdateOrderIndices();
        }
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (Images.Count == 0)
        {
            _dialogService.ShowError("Validation Error", "Please add at least one image before converting.");
            return;
        }

        List<ImageItem> selectedImages = Images.Where(img => img.IsSelected).ToList();
        if (selectedImages.Count == 0)
        {
            _dialogService.ShowError("Validation Error", "Please select at least one image with the checkbox to include in the PDF.");
            return;
        }

        string? savePath = _dialogService.ShowSaveFileDialog("PDF Document (*.pdf)|*.pdf", ".pdf", "Save Compiled PDF");
        if (string.IsNullOrEmpty(savePath))
        {
            return;
        }

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = "Compiling PDF document...";
        _cts = new CancellationTokenSource();

        Progress<ConversionProgress> progress = new Progress<ConversionProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            ImageToPdfOptions options = new ImageToPdfOptions
            {
                OutputPdfPath = savePath,
                PageSize = SelectedPageSize,
                Orientation = SelectedOrientation,
                MarginPoints = MarginPoints
            };

            await _imageToPdfService.ConvertImagesToPdfAsync(selectedImages, options, progress, _cts.Token);
            StatusMessage = "PDF generated successfully!";
            _dialogService.ShowMessage("Conversion Complete", $"PDF successfully created at:\n{savePath}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "PDF generation cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to generate PDF.";
            _dialogService.ShowError("PDF Generation Error", ex.Message);
        }
        finally
        {
            IsConverting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            StatusMessage = "Cancelling PDF compilation...";
            _cts.Cancel();
        }
    }

    #endregion

    #region Private Helper Methods

    private void ImageItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageItem.IsSelected))
        {
            UpdateSelectedImageCount();
        }
    }

    private void UpdateSelectedImageCount()
    {
        SelectedImageCount = Images.Count(img => img.IsSelected);
        StatusMessage = Images.Count > 0 
            ? $"{SelectedImageCount} of {Images.Count} image(s) selected for PDF compilation."
            : "Ready";
    }

    private void UpdateOrderIndices()
    {
        for (int i = 0; i < Images.Count; i++)
        {
            Images[i].OrderIndex = i + 1;
        }
    }

    #endregion
}
