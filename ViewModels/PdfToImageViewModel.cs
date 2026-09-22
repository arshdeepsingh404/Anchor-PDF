using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Anchor_PDF.Models;
using Anchor_PDF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class PdfToImageViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IPdfToImageService _pdfToImageService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _conversionCts;
    private CancellationTokenSource? _thumbnailCts;
    private Guid _currentDocumentSessionId = Guid.Empty;

    public ObservableCollection<PdfPageItem> Pages { get; } = [];

    public IReadOnlyList<ImageFormatType> AvailableFormats { get; } = 
        [ImageFormatType.PNG, ImageFormatType.JPEG, ImageFormatType.WEBP];

    public IReadOnlyList<int> AvailableDpiList { get; } = [96, 150, 300, 600, 1200];

    public PdfToImageViewModel(IPdfToImageService pdfToImageService, IDialogService dialogService)
    {
        _pdfToImageService = pdfToImageService;
        _dialogService = dialogService;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PdfFileName))]
    [NotifyPropertyChangedFor(nameof(HasSelectedPdf))]
    private string? _selectedPdfPath;

    public bool HasSelectedPdf => !string.IsNullOrEmpty(SelectedPdfPath);

    public string PdfFileName => !string.IsNullOrEmpty(SelectedPdfPath) 
        ? Path.GetFileName(SelectedPdfPath) 
        : string.Empty;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _selectedPageCount;

    [ObservableProperty]
    private bool _hasPages;

    [ObservableProperty]
    private string _pageRange = "All";

    [ObservableProperty]
    private ImageFormatType _selectedFormat = ImageFormatType.PNG;

    [ObservableProperty]
    private int _selectedDpi = 300;

    [ObservableProperty]
    private bool _exportAsZip;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isConverting;



    [RelayCommand]
    private async Task SelectPdfAsync()
    {
        string? file = _dialogService.ShowOpenFileDialog("PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*", "Select PDF File");
        if (!string.IsNullOrEmpty(file))
        {
            await SetSelectedPdfAsync(file);
        }
    }


    [RelayCommand]
    private void SelectAllPages()
    {
        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = true;
        }

        UpdateSelectedPageCount();
    }

    [RelayCommand]
    private void DeselectAllPages()
    {
        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = false;
        }

        UpdateSelectedPageCount();
    }

    [RelayCommand]
    private void InvertPageSelection()
    {
        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = !page.IsSelected;
        }

        UpdateSelectedPageCount();
    }

    [RelayCommand]
    private void TogglePageSelection(PdfPageItem? item)
    {
        if (item == null)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
        UpdateSelectedPageCount();
    }

    [RelayCommand]
    private void ApplyPageRange()
    {
        if (Pages.Count == 0 || string.IsNullOrWhiteSpace(PageRange))
        {
            return;
        }

        List<int> validPages = _pdfToImageService.ParsePageRange(PageRange, TotalPages);
        HashSet<int> selectedSet = new HashSet<int>(validPages);

        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = selectedSet.Contains(page.PageNumber);
        }

        UpdateSelectedPageCount();
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (string.IsNullOrEmpty(SelectedPdfPath) || !File.Exists(SelectedPdfPath))
        {
            _dialogService.ShowError("Validation Error", "Please select a valid PDF file first.");
            return;
        }

        List<int> selectedPageNumbers = Pages
            .Where(p => p.IsSelected)
            .Select(p => p.PageNumber)
            .OrderBy(p => p)
            .ToList();

        if (selectedPageNumbers.Count == 0)
        {
            _dialogService.ShowError("Validation Error", "Please select at least one page to convert.");
            return;
        }

        // Cancel background thumbnail generation to allocate full resources to export
        CancelThumbnailGeneration();

        string? destinationFolder = _dialogService.ShowFolderBrowserDialog("Select Destination Folder for Export");
        if (string.IsNullOrEmpty(destinationFolder))
        {
            return;
        }

        if (!Directory.Exists(destinationFolder))
        {
            try
            {
                Directory.CreateDirectory(destinationFolder);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Destination Error", $"Cannot create destination folder:\n{ex.Message}");
                return;
            }
        }

        bool autoZip = selectedPageNumbers.Count > 1;
        string pdfBaseName = Path.GetFileNameWithoutExtension(SelectedPdfPath);
        string? zipPath = autoZip 
            ? Path.Combine(destinationFolder, $"{pdfBaseName}_images.zip")
            : null;

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = autoZip ? "Packaging images into ZIP archive..." : "Exporting page image...";
        _conversionCts = new CancellationTokenSource();

        Progress<ConversionProgress> progress = new Progress<ConversionProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            PdfToImageOptions options = new PdfToImageOptions
            {
                SourcePdfPath = SelectedPdfPath,
                OutputDirectory = destinationFolder,
                Format = SelectedFormat,
                Dpi = SelectedDpi,
                SpecificPages = selectedPageNumbers,
                ExportAsZip = autoZip,
                ZipFilePath = zipPath
            };

            await _pdfToImageService.ConvertPdfToImagesAsync(options, progress, _conversionCts.Token);

            string savedPath = options.GeneratedFiles.FirstOrDefault() 
                ?? (autoZip ? (options.ZipFilePath ?? zipPath!) : destinationFolder);

            if (autoZip)
            {
                StatusMessage = "Images exported to ZIP archive!";
                _dialogService.ShowMessage("Export Complete", $"Successfully exported {selectedPageNumbers.Count} pages to ZIP archive:\n\n{savedPath}");
            }
            else
            {
                StatusMessage = "Page image exported successfully!";
                _dialogService.ShowMessage("Export Complete", $"Successfully exported page {selectedPageNumbers[0]} to:\n\n{savedPath}");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Conversion cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Conversion failed.";
            _dialogService.ShowError("Conversion Error", ex.Message);
        }
        finally
        {
            IsConverting = false;
            _conversionCts?.Dispose();
            _conversionCts = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_conversionCts != null && !_conversionCts.IsCancellationRequested)
        {
            StatusMessage = "Cancelling conversion...";
            _conversionCts.Cancel();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the chosen PDF, parses total page count, creates card placeholders, and loads thumbnails asynchronously.
    /// </summary>
    public async Task SetSelectedPdfAsync(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            _dialogService.ShowError("Invalid File", "Please select a valid PDF file.");
            return;
        }

        // Cancel previous thumbnail work safely without disposing active CTS while in flight
        CancelThumbnailGeneration();
        _thumbnailCts = new CancellationTokenSource();
        CancellationToken cancellationToken = _thumbnailCts.Token;

        Guid sessionId = Guid.NewGuid();
        _currentDocumentSessionId = sessionId;

        SelectedPdfPath = filePath;
        StatusMessage = "Reading PDF information...";

        try
        {
            int pageCount = await _pdfToImageService.GetPageCountAsync(filePath, cancellationToken);
            if (sessionId != _currentDocumentSessionId || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            TotalPages = pageCount;
            StatusMessage = $"Loaded PDF: {TotalPages} page(s).";


            // Unsubscribe existing page items to prevent memory leaks
            ClearPages();

            // Populate placeholders
            for (int i = 1; i <= TotalPages; i++)
            {
                PdfPageItem pageItem = new PdfPageItem
                {
                    PageNumber = i,
                    IsSelected = true,
                    IsLoading = true
                };

                pageItem.PropertyChanged += PageItem_PropertyChanged;
                Pages.Add(pageItem);
            }

            HasPages = Pages.Count > 0;
            UpdateSelectedPageCount();

            // Stream thumbnails in background using immutable page numbers and session check
            StartThumbnailStreaming(filePath, TotalPages, sessionId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected cancellation
        }
        catch (Exception ex)
        {
            StatusMessage = "Error reading PDF.";
            _dialogService.ShowError("Error Reading PDF", ex.Message);
        }
    }

    #endregion

    #region Private Helper Methods

    private void StartThumbnailStreaming(string filePath, int totalPageCount, Guid sessionId, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            for (int pageNumber = 1; pageNumber <= totalPageCount; pageNumber++)
            {
                if (cancellationToken.IsCancellationRequested || sessionId != _currentDocumentSessionId)
                {
                    break;
                }

                BitmapSource? thumbnail = null;
                try
                {
                    thumbnail = await _pdfToImageService.RenderPageThumbnailAsync(filePath, pageNumber, 50, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Continue rendering remaining pages if a single page fails
                }

                if (thumbnail != null && !cancellationToken.IsCancellationRequested && sessionId == _currentDocumentSessionId)
                {
                    int currentPageNumber = pageNumber;
                    BitmapSource frozenThumbnail = thumbnail;

                    App.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        if (cancellationToken.IsCancellationRequested || sessionId != _currentDocumentSessionId)
                        {
                            return;
                        }

                        int index = currentPageNumber - 1;
                        if (index >= 0 && index < Pages.Count)
                        {
                            PdfPageItem pageItem = Pages[index];
                            if (pageItem.PageNumber == currentPageNumber)
                            {
                                pageItem.Thumbnail = frozenThumbnail;
                                pageItem.PixelWidth = frozenThumbnail.PixelWidth;
                                pageItem.PixelHeight = frozenThumbnail.PixelHeight;
                                pageItem.IsLoading = false;
                            }
                        }
                    }, DispatcherPriority.Background);
                }

                try
                {
                    await Task.Delay(20, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, cancellationToken);
    }

    private void CancelThumbnailGeneration()
    {
        if (_thumbnailCts != null)
        {
            try
            {
                if (!_thumbnailCts.IsCancellationRequested)
                {
                    _thumbnailCts.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
            _thumbnailCts = null;
        }
    }

    private void ClearPages()
    {
        foreach (PdfPageItem item in Pages)
        {
            item.PropertyChanged -= PageItem_PropertyChanged;
        }
        Pages.Clear();
    }

    private void PageItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfPageItem.IsSelected))
        {
            UpdateSelectedPageCount();
        }
    }

    private void UpdateSelectedPageCount()
    {
        SelectedPageCount = Pages.Count(p => p.IsSelected);
        StatusMessage = $"{SelectedPageCount} of {TotalPages} page(s) selected.";
    }

    #endregion
}
