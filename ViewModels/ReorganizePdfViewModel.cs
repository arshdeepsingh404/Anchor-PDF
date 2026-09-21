using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Anchor_PDF.Models;
using Anchor_PDF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class ReorganizePdfViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IPdfReorganizeService _pdfReorganizeService;
    private readonly IPdfToImageService _pdfToImageService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _conversionCts;
    private CancellationTokenSource? _thumbnailCts;
    private Guid _currentDocumentSessionId = Guid.Empty;

    public ObservableCollection<ReorganizePageItem> Pages { get; } = [];

    public ReorganizePdfViewModel(
        IPdfReorganizeService pdfReorganizeService,
        IPdfToImageService pdfToImageService,
        IDialogService dialogService)
    {
        _pdfReorganizeService = pdfReorganizeService;
        _pdfToImageService = pdfToImageService;
        _dialogService = dialogService;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPdf))]
    [NotifyPropertyChangedFor(nameof(PdfFileName))]
    [NotifyPropertyChangedFor(nameof(CanConvert))]
    private string? _selectedPdfPath;

    public bool HasSelectedPdf => !string.IsNullOrEmpty(SelectedPdfPath);

    public string PdfFileName => !string.IsNullOrEmpty(SelectedPdfPath) 
        ? Path.GetFileName(SelectedPdfPath) 
        : string.Empty;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPage))]
    [NotifyPropertyChangedFor(nameof(CanMoveLeft))]
    [NotifyPropertyChangedFor(nameof(CanMoveRight))]
    private ReorganizePageItem? _selectedPage;

    public bool HasSelectedPage => SelectedPage != null && !IsConverting;
    public bool CanMoveLeft => SelectedPage != null && Pages.IndexOf(SelectedPage) > 0 && !IsConverting;
    public bool CanMoveRight => SelectedPage != null && Pages.IndexOf(SelectedPage) >= 0 && Pages.IndexOf(SelectedPage) < Pages.Count - 1 && !IsConverting;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConvert))]
    [NotifyPropertyChangedFor(nameof(HasSelectedPage))]
    [NotifyPropertyChangedFor(nameof(CanMoveLeft))]
    [NotifyPropertyChangedFor(nameof(CanMoveRight))]
    private bool _isConverting;

    public bool CanConvert => Pages.Count > 0 && !IsConverting;

    #endregion

    #region Relay Commands

    [RelayCommand]
    private async Task SelectPdfAsync()
    {
        string? file = _dialogService.ShowOpenFileDialog("PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*", "Select PDF to Reorganize");
        if (!string.IsNullOrEmpty(file))
        {
            await SetSelectedPdfAsync(file);
        }
    }

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
        StatusMessage = "Reading PDF page layout...";

        try
        {
            int pageCount = await _pdfReorganizeService.GetPageCountAsync(filePath, cancellationToken);
            if (sessionId != _currentDocumentSessionId || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            TotalPages = pageCount;
            Pages.Clear();

            for (int i = 1; i <= pageCount; i++)
            {
                ReorganizePageItem pageItem = new ReorganizePageItem
                {
                    OriginalPageNumber = i,
                    CurrentOrderIndex = i,
                    RotationAngle = 0,
                    IsLoading = true
                };

                Pages.Add(pageItem);
            }

            if (Pages.Count > 0)
            {
                SelectPageItem(Pages[0]);
            }

            OnPropertyChanged(nameof(CanConvert));
            StatusMessage = $"Loaded {PdfFileName} ({TotalPages} pages). Select pages to rotate or reorder.";

            StartThumbnailStreaming(filePath, pageCount, sessionId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            StatusMessage = "Error reading PDF.";
            _dialogService.ShowError("Error Reading PDF", ex.Message);
        }
    }

    [RelayCommand]
    private void SelectPageItem(ReorganizePageItem? item)
    {
        if (item == null)
        {
            return;
        }

        foreach (ReorganizePageItem p in Pages)
        {
            p.IsSelected = false;
        }

        item.IsSelected = true;
        SelectedPage = item;
        OnPropertyChanged(nameof(CanMoveLeft));
        OnPropertyChanged(nameof(CanMoveRight));
        OnPropertyChanged(nameof(HasSelectedPage));
    }

    [RelayCommand]
    private void MoveLeft(ReorganizePageItem? target)
    {
        ReorganizePageItem? item = target ?? SelectedPage;
        if (item == null)
        {
            return;
        }

        int index = Pages.IndexOf(item);
        if (index > 0)
        {
            Pages.Move(index, index - 1);
            ReindexPages();
            SelectPageItem(item);
        }
    }

    [RelayCommand]
    private void MoveRight(ReorganizePageItem? target)
    {
        ReorganizePageItem? item = target ?? SelectedPage;
        if (item == null)
        {
            return;
        }

        int index = Pages.IndexOf(item);
        if (index >= 0 && index < Pages.Count - 1)
        {
            Pages.Move(index, index + 1);
            ReindexPages();
            SelectPageItem(item);
        }
    }

    [RelayCommand]
    private void RotateClockwise(ReorganizePageItem? target)
    {
        ReorganizePageItem? item = target ?? SelectedPage;
        if (item == null)
        {
            return;
        }

        item.RotationAngle = (item.RotationAngle + 90) % 360;
        SelectPageItem(item);
    }

    [RelayCommand]
    private void RotateCounterClockwise(ReorganizePageItem? target)
    {
        ReorganizePageItem? item = target ?? SelectedPage;
        if (item == null)
        {
            return;
        }

        item.RotationAngle = (item.RotationAngle + 270) % 360;
        SelectPageItem(item);
    }

    [RelayCommand]
    private void DeletePage(ReorganizePageItem? target)
    {
        ReorganizePageItem? item = target ?? SelectedPage;
        if (item == null)
        {
            return;
        }

        int index = Pages.IndexOf(item);
        Pages.Remove(item);
        ReindexPages();

        if (Pages.Count > 0)
        {
            int nextIndex = Math.Min(index, Pages.Count - 1);
            SelectPageItem(Pages[nextIndex]);
        }
        else
        {
            SelectedPage = null;
        }

        OnPropertyChanged(nameof(CanConvert));
        StatusMessage = $"{Pages.Count} page(s) remaining in document.";
    }

    [RelayCommand]
    private void DuplicatePage(ReorganizePageItem? target)
    {
        ReorganizePageItem? item = target ?? SelectedPage;
        if (item == null)
        {
            return;
        }

        int index = Pages.IndexOf(item);
        ReorganizePageItem duplicate = new ReorganizePageItem
        {
            OriginalPageNumber = item.OriginalPageNumber,
            CurrentOrderIndex = index + 2,
            RotationAngle = item.RotationAngle,
            Thumbnail = item.Thumbnail,
            IsLoading = item.IsLoading
        };

        Pages.Insert(index + 1, duplicate);
        ReindexPages();
        SelectPageItem(duplicate);
        OnPropertyChanged(nameof(CanConvert));
        StatusMessage = $"Duplicated page {item.OriginalPageNumber}. Total pages: {Pages.Count}.";
    }

    [RelayCommand]
    private async Task ResetOrderAsync()
    {
        if (!string.IsNullOrEmpty(SelectedPdfPath) && File.Exists(SelectedPdfPath))
        {
            await SetSelectedPdfAsync(SelectedPdfPath);
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

        if (Pages.Count == 0)
        {
            _dialogService.ShowError("Validation Error", "Document has no pages remaining.");
            return;
        }

        CancelThumbnailGeneration();

        string defaultName = $"{Path.GetFileNameWithoutExtension(SelectedPdfPath)}_organized.pdf";
        string? targetPath = _dialogService.ShowSaveFileDialog(
            "PDF Document (*.pdf)|*.pdf",
            "Save Reorganized PDF Document",
            defaultName);

        if (string.IsNullOrEmpty(targetPath))
        {
            return;
        }

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = "Applying page sequence and rotations...";
        _conversionCts = new CancellationTokenSource();

        Progress<ConversionProgress> progress = new Progress<ConversionProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            await _pdfReorganizeService.ReorganizePdfAsync(
                SelectedPdfPath,
                Pages.ToList(),
                targetPath,
                progress,
                _conversionCts.Token);

            _dialogService.ShowMessage(
                "Reorganize Complete",
                $"Successfully saved {Pages.Count} pages to:\n\n{targetPath}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Reorganization cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Reorganization failed.";
            _dialogService.ShowError("Reorganization Error", ex.Message);
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
            StatusMessage = "Cancelling reorganization...";
            _conversionCts.Cancel();
        }
    }

    #endregion

    #region Private Helper Methods

    private void ReindexPages()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].CurrentOrderIndex = i + 1;
        }

        OnPropertyChanged(nameof(CanMoveLeft));
        OnPropertyChanged(nameof(CanMoveRight));
        OnPropertyChanged(nameof(HasSelectedPage));
    }

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
                    // Ignore per-page thumbnail load error
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

                        foreach (ReorganizePageItem pageItem in Pages)
                        {
                            if (pageItem.OriginalPageNumber == currentPageNumber && pageItem.Thumbnail == null)
                            {
                                pageItem.Thumbnail = frozenThumbnail;
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
                // Disposed
            }
            _thumbnailCts = null;
        }
    }

    #endregion
}
