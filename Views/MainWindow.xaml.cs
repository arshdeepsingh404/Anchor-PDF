using System.IO;
using System.Windows;
using Anchor_PDF.ViewModels;

namespace Anchor_PDF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region Fields and Constructor

    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    #endregion

    #region Drag and Drop Handling

    /// <summary>
    /// Handles drag over event to indicate copy cursor when files are dragged.
    /// </summary>
    private void Window_DragOver(object sender, DragEventArgs eventArgs)
    {
        if (eventArgs.Data.GetDataPresent(DataFormats.FileDrop))
        {
            eventArgs.Effects = DragDropEffects.Copy;
            eventArgs.Handled = true;
        }
        else
        {
            eventArgs.Effects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// Handles drop of PDF or image files directly onto the window.
    /// </summary>
    private async void Window_Drop(object sender, DragEventArgs eventArgs)
    {
        try
        {
            if (!eventArgs.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[]? droppedEntries = eventArgs.Data.GetData(DataFormats.FileDrop) as string[];
            if (droppedEntries == null || droppedEntries.Length == 0)
            {
                return;
            }

            if (DataContext is not MainViewModel viewModel)
            {
                return;
            }

            if (viewModel.IsPdfToImageActive)
            {
                string? pdfFile = droppedEntries.FirstOrDefault(entry => entry.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(pdfFile) && File.Exists(pdfFile))
                {
                    await viewModel.PdfToImage.SetSelectedPdfAsync(pdfFile);
                }
            }
            else if (viewModel.IsImageToPdfActive)
            {
                List<string> imageFiles = droppedEntries
                    .Where(entry => File.Exists(entry) && SupportedImageExtensions.Contains(Path.GetExtension(entry)))
                    .ToList();

                if (imageFiles.Count > 0)
                {
                    await viewModel.ImageToPdf.AddFilesAsync(imageFiles);
                }
            }
            else if (viewModel.IsMergePdfActive)
            {
                List<string> pdfFiles = droppedEntries
                    .Where(entry => File.Exists(entry) && entry.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (pdfFiles.Count > 0)
                {
                    await viewModel.MergePdf.AddFilesListAsync(pdfFiles);
                }
            }
            else if (viewModel.IsSplitPdfActive)
            {
                string? pdfFile = droppedEntries.FirstOrDefault(entry => entry.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(pdfFile) && File.Exists(pdfFile))
                {
                    await viewModel.SplitPdf.SetSelectedPdfAsync(pdfFile);
                }
            }
            else if (viewModel.IsReorganizePdfActive)
            {
                string? pdfFile = droppedEntries.FirstOrDefault(entry => entry.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(pdfFile) && File.Exists(pdfFile))
                {
                    await viewModel.ReorganizePdf.SetSelectedPdfAsync(pdfFile);
                }
            }
            else if (viewModel.IsCompressPdfActive)
            {
                string? pdfFile = droppedEntries.FirstOrDefault(entry => entry.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(pdfFile) && File.Exists(pdfFile))
                {
                    await viewModel.CompressPdf.SetSelectedPdfAsync(pdfFile);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error opening dropped files:\n\n{ex.Message}",
                "Drop Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    #endregion
}
