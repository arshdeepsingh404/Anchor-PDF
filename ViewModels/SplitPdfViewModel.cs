using System.IO;
using Anchor_PDF.Models;
using Anchor_PDF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class SplitPdfViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IPdfSplitService _pdfSplitService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cts;

    public IReadOnlyList<SplitMode> AvailableSplitModes { get; } = 
        [SplitMode.AllPages, SplitMode.CustomRange, SplitMode.FixedInterval];

    public SplitPdfViewModel(IPdfSplitService pdfSplitService, IDialogService dialogService)
    {
        _pdfSplitService = pdfSplitService;
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
    [NotifyPropertyChangedFor(nameof(IsAllPagesMode))]
    [NotifyPropertyChangedFor(nameof(IsCustomRangeMode))]
    [NotifyPropertyChangedFor(nameof(IsFixedIntervalMode))]
    private SplitMode _selectedSplitMode = SplitMode.AllPages;

    public bool IsAllPagesMode => SelectedSplitMode == SplitMode.AllPages;
    public bool IsCustomRangeMode => SelectedSplitMode == SplitMode.CustomRange;
    public bool IsFixedIntervalMode => SelectedSplitMode == SplitMode.FixedInterval;

    [ObservableProperty]
    private string _rangeExpression = "1-3";

    [ObservableProperty]
    private int _pagesPerChunk = 2;

    [ObservableProperty]
    private bool _combineIntoSingleFile = true;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    private bool _isConverting;

    public bool CanSplit => HasSelectedPdf && !IsConverting;

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

    public async Task SetSelectedPdfAsync(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            _dialogService.ShowError("Invalid File", "Please select a valid PDF file.");
            return;
        }

        SelectedPdfPath = filePath;
        StatusMessage = "Reading PDF page count...";

        try
        {
            TotalPages = await _pdfSplitService.GetPageCountAsync(filePath);
            RangeExpression = TotalPages > 1 ? $"1-{Math.Min(3, TotalPages)}" : "1";
            StatusMessage = $"Loaded {PdfFileName} ({TotalPages} pages). Ready to split.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error reading PDF.";
            _dialogService.ShowError("PDF Read Error", ex.Message);
        }
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

        string? destinationFolder = _dialogService.ShowFolderBrowserDialog("Select Destination Folder for Split PDF Files");
        if (string.IsNullOrEmpty(destinationFolder))
        {
            return;
        }

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = "Preparing to split PDF...";
        _cts = new CancellationTokenSource();

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
                        _cts.Token);
                    break;

                case SplitMode.CustomRange:
                    await _pdfSplitService.SplitByRangeAsync(
                        SelectedPdfPath,
                        destinationFolder,
                        RangeExpression,
                        CombineIntoSingleFile,
                        progress,
                        _cts.Token);
                    break;

                case SplitMode.FixedInterval:
                    await _pdfSplitService.SplitEveryNPagesAsync(
                        SelectedPdfPath,
                        destinationFolder,
                        PagesPerChunk,
                        progress,
                        _cts.Token);
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
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            StatusMessage = "Cancelling split operation...";
            _cts.Cancel();
        }
    }

    #endregion
}
