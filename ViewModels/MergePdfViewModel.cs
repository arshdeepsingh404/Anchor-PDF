using System.Collections.ObjectModel;
using System.IO;
using Anchor_PDF.Models;
using Anchor_PDF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class MergePdfViewModel : ObservableObject
{
    #region Fields and Constructor

    private readonly IPdfMergeService _pdfMergeService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cts;

    public ObservableCollection<MergePdfItem> Files { get; } = [];

    public MergePdfViewModel(IPdfMergeService pdfMergeService, IDialogService dialogService)
    {
        _pdfMergeService = pdfMergeService;
        _dialogService = dialogService;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    private MergePdfItem? _selectedFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanMerge))]
    private int _totalFilesCount;

    [ObservableProperty]
    private int _totalPageCount;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanMerge))]
    private bool _isConverting;

    public bool HasFiles => Files.Count > 0;

    public bool CanMerge => Files.Count >= 2 && !IsConverting;

    #endregion

    #region Relay Commands

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        string filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
        string[]? selectedFiles = _dialogService.ShowOpenFilesDialog(filter, "Select PDF Files to Merge");
        if (selectedFiles != null && selectedFiles.Length > 0)
        {
            await AddFilesListAsync(selectedFiles);
        }
    }

    public async Task AddFilesListAsync(IEnumerable<string> filePaths)
    {
        StatusMessage = "Loading PDF files...";
        int addedCount = 0;

        foreach (string path in filePaths)
        {
            if (!File.Exists(path) || !path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                int orderIndex = Files.Count + 1;
                MergePdfItem item = await _pdfMergeService.LoadPdfItemAsync(path, orderIndex);
                Files.Add(item);
                addedCount++;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error Loading File", $"Failed to load '{Path.GetFileName(path)}':\n{ex.Message}");
            }
        }

        ReindexFiles();
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(CanMerge));
        StatusMessage = $"{Files.Count} PDF document(s) ready ({TotalPageCount} total pages).";
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedFile == null)
        {
            return;
        }

        int index = Files.IndexOf(SelectedFile);
        Files.Remove(SelectedFile);

        if (Files.Count > 0)
        {
            int nextIndex = Math.Min(index, Files.Count - 1);
            SelectedFile = Files[nextIndex];
        }
        else
        {
            SelectedFile = null;
        }

        ReindexFiles();
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(CanMerge));
        StatusMessage = $"{Files.Count} PDF document(s) remaining.";
    }

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedFile == null)
        {
            return;
        }

        int currentIndex = Files.IndexOf(SelectedFile);
        if (currentIndex > 0)
        {
            Files.Move(currentIndex, currentIndex - 1);
            ReindexFiles();
        }
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedFile == null)
        {
            return;
        }

        int currentIndex = Files.IndexOf(SelectedFile);
        if (currentIndex >= 0 && currentIndex < Files.Count - 1)
        {
            Files.Move(currentIndex, currentIndex + 1);
            ReindexFiles();
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        Files.Clear();
        SelectedFile = null;
        ReindexFiles();
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(CanMerge));
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (Files.Count < 2)
        {
            _dialogService.ShowError("Validation Error", "Please add at least 2 PDF files to merge.");
            return;
        }

        string defaultName = $"Merged_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        string? targetPath = _dialogService.ShowSaveFileDialog(
            "PDF Document (*.pdf)|*.pdf",
            "Save Merged PDF Document",
            defaultName);

        if (string.IsNullOrEmpty(targetPath))
        {
            return;
        }

        IsConverting = true;
        ProgressPercentage = 0;
        StatusMessage = "Preparing to merge PDF documents...";
        _cts = new CancellationTokenSource();

        Progress<ConversionProgress> progress = new Progress<ConversionProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            StatusMessage = p.Message;
        });

        try
        {
            await _pdfMergeService.MergePdfsAsync(
                Files.ToList(),
                targetPath,
                progress,
                _cts.Token);

            _dialogService.ShowMessage(
                "Merge Complete",
                $"Successfully merged {Files.Count} documents into:\n\n{targetPath}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Merge cancelled by user.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Merge failed.";
            _dialogService.ShowError("Merge Error", ex.Message);
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
            StatusMessage = "Cancelling merge operation...";
            _cts.Cancel();
        }
    }

    #endregion

    #region Private Helper Methods

    private void ReindexFiles()
    {
        int totalPages = 0;
        for (int i = 0; i < Files.Count; i++)
        {
            Files[i].OrderIndex = i + 1;
            totalPages += Files[i].PageCount;
        }

        TotalFilesCount = Files.Count;
        TotalPageCount = totalPages;
    }

    #endregion
}
