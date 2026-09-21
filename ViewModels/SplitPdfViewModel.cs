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

public partial class SplitPdfViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IPdfSplitService _pdfSplitService;
    private readonly IPdfToImageService _pdfToImageService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _conversionCts;
    private CancellationTokenSource? _thumbnailCts;
    private Guid _currentDocumentSessionId = Guid.Empty;
    private bool _isUpdatingInternally;

    public ObservableCollection<PdfPageItem> Pages { get; } = [];

    public IReadOnlyList<SplitMode> AvailableSplitModes { get; } = 
        [SplitMode.AllPages, SplitMode.CustomRange];

    public SplitPdfViewModel(
        IPdfSplitService pdfSplitService, 
        IPdfToImageService pdfToImageService, 
        IDialogService dialogService)
    {
        _pdfSplitService = pdfSplitService;
        _pdfToImageService = pdfToImageService;
        _dialogService = dialogService;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPdf))]
    [NotifyPropertyChangedFor(nameof(PdfFileName))]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    private string? _selectedPdfPath;

    public bool HasSelectedPdf => !string.IsNullOrEmpty(SelectedPdfPath);

    public string PdfFileName => !string.IsNullOrEmpty(SelectedPdfPath) 
        ? Path.GetFileName(SelectedPdfPath) 
        : string.Empty;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    private int _selectedPageCount;

    [ObservableProperty]
    private bool _hasPages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAllPagesMode))]
    [NotifyPropertyChangedFor(nameof(IsCustomRangeMode))]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    private SplitMode _selectedSplitMode = SplitMode.AllPages;

    public bool IsAllPagesMode => SelectedSplitMode == SplitMode.AllPages;
    public bool IsCustomRangeMode => SelectedSplitMode == SplitMode.CustomRange;

    [ObservableProperty]
    private string _rangeExpression = "1";

    [ObservableProperty]
    private bool _combineIntoSingleFile = true;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    private bool _isConverting;

    public bool CanSplit => HasSelectedPdf && !IsConverting && (IsAllPagesMode ? TotalPages > 0 : SelectedPageCount > 0);

    #endregion

    #region Property Change Handlers

    partial void OnRangeExpressionChanged(string value)
    {
        if (_isUpdatingInternally || Pages.Count == 0)
        {
            return;
        }

        SyncSelectionFromRange(value);
    }

    partial void OnSelectedSplitModeChanged(SplitMode value)
    {
        if (value == SplitMode.AllPages && Pages.Count > 0)
        {
            SelectAllPages();
        }
    }

    #endregion

    #region Relay Commands

    [RelayCommand]
    private async Task SelectPdfAsync()
    {
        string? file = _dialogService.ShowOpenFileDialog("PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*", "Select PDF to Split");
        if (!string.IsNullOrEmpty(file))
        {
            await SetSelectedPdfAsync(file);
        }
    }

    [RelayCommand]
    private void SelectAllPages()
    {
        _isUpdatingInternally = true;
        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = true;
        }

        RangeExpression = TotalPages > 1 ? $"1-{TotalPages}" : "1";
        _isUpdatingInternally = false;
        UpdateCounts();
    }

    [RelayCommand]
    private void DeselectAllPages()
    {
        _isUpdatingInternally = true;
        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = false;
        }

        RangeExpression = string.Empty;
        _isUpdatingInternally = false;
        UpdateCounts();
    }

    [RelayCommand]
    private void TogglePageSelection(PdfPageItem? item)
    {
        if (item == null)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
    }

    [RelayCommand]
    private void SetSplitMode(string modeName)
    {
        if (Enum.TryParse<SplitMode>(modeName, true, out SplitMode mode))
        {
            SelectedSplitMode = mode;
        }
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (string.IsNullOrEmpty(SelectedPdfPath) || !File.Exists(SelectedPdfPath))
        {
            _dialogService.ShowError("Validation Error", "Please select a valid PDF file first.");
            return;
        }

        if (IsCustomRangeMode && SelectedPageCount == 0)
        {
            _dialogService.ShowError("Validation Error", "Please select at least one page to extract.");
            return;
        }

        CancelThumbnailGeneration();

        string? destinationFolder = _dialogService.ShowFolderBrowserDialog("Select Destination Folder for Split PDF Files");
        if (string.IsNullOrEmpty(destinationFolder))
        {
            return;
        }

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = "Preparing to split PDF...";
        _conversionCts = new CancellationTokenSource();

        Progress<ConversionProgress> progress = new Progress<ConversionProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            switch (SelectedSplitMode)
            {
                case SplitMode.AllPages:
                    await _pdfSplitService.SplitAllPagesAsync(
                        SelectedPdfPath,
                        destinationFolder,
                        progress,
                        _conversionCts.Token);
                    break;

                case SplitMode.CustomRange:
                    await _pdfSplitService.SplitByRangeAsync(
                        SelectedPdfPath,
                        destinationFolder,
                        RangeExpression,
                        CombineIntoSingleFile,
                        progress,
                        _conversionCts.Token);
                    break;
            }

            _dialogService.ShowMessage(
                "Split Complete",
                $"Successfully split document into destination folder:\n\n{destinationFolder}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Split cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Split operation failed.";
            _dialogService.ShowError("Split Error", ex.Message);
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
            StatusMessage = "Cancelling split operation...";
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

        CancelThumbnailGeneration();
        _thumbnailCts = new CancellationTokenSource();
        CancellationToken cancellationToken = _thumbnailCts.Token;

        Guid sessionId = Guid.NewGuid();
        _currentDocumentSessionId = sessionId;

        SelectedPdfPath = filePath;
        StatusMessage = "Reading PDF page count...";

        try
        {
            TotalPages = await _pdfSplitService.GetPageCountAsync(filePath, cancellationToken);
            if (sessionId != _currentDocumentSessionId || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            ClearPages();

            _isUpdatingInternally = true;
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

            RangeExpression = TotalPages > 1 ? $"1-{TotalPages}" : "1";
            _isUpdatingInternally = false;

            HasPages = Pages.Count > 0;
            UpdateCounts();
            StatusMessage = $"Loaded {PdfFileName} ({TotalPages} pages). Ready to split.";

            StartThumbnailStreaming(filePath, TotalPages, sessionId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected cancellation
        }
        catch (Exception ex)
        {
            StatusMessage = "Error reading PDF.";
            _dialogService.ShowError("PDF Read Error", ex.Message);
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
        if (e.PropertyName == nameof(PdfPageItem.IsSelected) && !_isUpdatingInternally)
        {
            SyncRangeFromSelection();
        }
    }

    private void SyncRangeFromSelection()
    {
        List<int> selectedPages = Pages
            .Where(p => p.IsSelected)
            .Select(p => p.PageNumber)
            .OrderBy(p => p)
            .ToList();

        if (SelectedSplitMode == SplitMode.AllPages && selectedPages.Count < TotalPages)
        {
            SelectedSplitMode = SplitMode.CustomRange;
        }

        _isUpdatingInternally = true;
        RangeExpression = BuildRangeExpression(selectedPages);
        _isUpdatingInternally = false;

        UpdateCounts();
    }

    private void SyncSelectionFromRange(string rangeString)
    {
        List<int> validPages = _pdfToImageService.ParsePageRange(rangeString, TotalPages);
        HashSet<int> selectedSet = new HashSet<int>(validPages);

        _isUpdatingInternally = true;
        foreach (PdfPageItem page in Pages)
        {
            page.IsSelected = selectedSet.Contains(page.PageNumber);
        }
        _isUpdatingInternally = false;

        UpdateCounts();
    }

    private static string BuildRangeExpression(List<int> sortedPages)
    {
        if (sortedPages.Count == 0)
        {
            return string.Empty;
        }

        List<string> ranges = [];
        int rangeStart = sortedPages[0];
        int previous = sortedPages[0];

        for (int i = 1; i < sortedPages.Count; i++)
        {
            int current = sortedPages[i];
            if (current == previous + 1)
            {
                previous = current;
                continue;
            }

            if (rangeStart == previous)
            {
                ranges.Add(rangeStart.ToString());
            }
            else
            {
                ranges.Add($"{rangeStart}-{previous}");
            }

            rangeStart = current;
            previous = current;
        }

        if (rangeStart == previous)
        {
            ranges.Add(rangeStart.ToString());
        }
        else
        {
            ranges.Add($"{rangeStart}-{previous}");
        }

        return string.Join(", ", ranges);
    }

    private void UpdateCounts()
    {
        SelectedPageCount = Pages.Count(p => p.IsSelected);
        if (IsAllPagesMode)
        {
            StatusMessage = $"All {TotalPages} pages will be extracted individually.";
        }
        else
        {
            StatusMessage = $"{SelectedPageCount} of {TotalPages} page(s) selected for extraction.";
        }
    }

    #endregion
}
