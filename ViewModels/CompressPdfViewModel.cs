using System.IO;
using Anchor_PDF.Models;
using Anchor_PDF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class CompressPdfViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IPdfCompressService _pdfCompressService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cts;

    public IReadOnlyList<CompressionPreset> AvailablePresets { get; } = 
        [CompressionPreset.Balanced, CompressionPreset.MaximumCompression, CompressionPreset.LosslessStreamOptimization];

    public CompressPdfViewModel(IPdfCompressService pdfCompressService, IDialogService dialogService)
    {
        _pdfCompressService = pdfCompressService;
        _dialogService = dialogService;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPdf))]
    [NotifyPropertyChangedFor(nameof(PdfFileName))]
    [NotifyPropertyChangedFor(nameof(CanCompress))]
    private string? _selectedPdfPath;

    public bool HasSelectedPdf => !string.IsNullOrEmpty(SelectedPdfPath);

    public string PdfFileName => !string.IsNullOrEmpty(SelectedPdfPath) 
        ? Path.GetFileName(SelectedPdfPath) 
        : string.Empty;

    [ObservableProperty]
    private long _originalSizeBytes;

    [ObservableProperty]
    private string _originalSizeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private string _compressedSizeText = string.Empty;

    [ObservableProperty]
    private string _savedPercentageText = string.Empty;

    public bool HasResults => !string.IsNullOrEmpty(CompressedSizeText);

    [ObservableProperty]
    private CompressionPreset _selectedPreset = CompressionPreset.Balanced;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompress))]
    private bool _isConverting;

    public bool CanCompress => HasSelectedPdf && !IsConverting;

    #endregion

    #region Relay Commands

    [RelayCommand]
    private async Task SelectPdfAsync()
    {
        string? file = _dialogService.ShowOpenFileDialog("PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*", "Select PDF to Compress");
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
        CompressedSizeText = string.Empty;
        SavedPercentageText = string.Empty;

        FileInfo fileInfo = new FileInfo(filePath);
        OriginalSizeBytes = fileInfo.Length;
        OriginalSizeText = FormatBytes(OriginalSizeBytes);
        StatusMessage = $"Selected {PdfFileName} ({OriginalSizeText}). Choose compression preset and click Convert.";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (string.IsNullOrEmpty(SelectedPdfPath) || !File.Exists(SelectedPdfPath))
        {
            _dialogService.ShowError("Validation Error", "Please select a valid PDF file first.");
            return;
        }

        string defaultName = $"{Path.GetFileNameWithoutExtension(SelectedPdfPath)}_compressed.pdf";
        string? targetPath = _dialogService.ShowSaveFileDialog(
            "PDF Document (*.pdf)|*.pdf",
            "Save Compressed PDF Document",
            defaultName);

        if (string.IsNullOrEmpty(targetPath))
        {
            return;
        }

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = "Starting document compression...";
        _cts = new CancellationTokenSource();

        Progress<ConversionProgress> progress = new Progress<ConversionProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            CompressionResult result = await _pdfCompressService.CompressPdfAsync(
                SelectedPdfPath,
                targetPath,
                SelectedPreset,
                progress,
                _cts.Token);

            CompressedSizeText = result.FormattedCompressedSize;
            SavedPercentageText = result.SavedPercentage > 0 
                ? $"-{result.SavedPercentage:F1}% ({result.FormattedSavedSize} saved)" 
                : "Optimized";

            StatusMessage = $"Compressed {result.FormattedOriginalSize} to {result.FormattedCompressedSize} ({SavedPercentageText})";

            _dialogService.ShowMessage(
                "Compression Complete",
                $"Successfully compressed PDF!\n\nOriginal: {result.FormattedOriginalSize}\nCompressed: {result.FormattedCompressedSize}\nSpace Saved: {result.FormattedSavedSize} ({result.SavedPercentage:F1}%)\n\nSaved to:\n{targetPath}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Compression cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Compression failed.";
            _dialogService.ShowError("Compression Error", ex.Message);
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
            StatusMessage = "Cancelling compression...";
            _cts.Cancel();
        }
    }

    #endregion

    #region Helper

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    #endregion
}
