using Anchor_PDF.Services.Implementations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anchor_PDF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region Fields and Constructor

    public PdfToImageViewModel PdfToImage { get; }
    public ImageToPdfViewModel ImageToPdf { get; }
    public MergePdfViewModel MergePdf { get; }
    public SplitPdfViewModel SplitPdf { get; }
    public ReorganizePdfViewModel ReorganizePdf { get; }

    public MainViewModel()
    {
        DialogService dialogService = new DialogService();
        PdfToImageService pdfToImageService = new PdfToImageService();
        ImageToPdfService imageToPdfService = new ImageToPdfService();
        PdfMergeService pdfMergeService = new PdfMergeService();
        PdfSplitService pdfSplitService = new PdfSplitService();
        PdfReorganizeService pdfReorganizeService = new PdfReorganizeService();

        PdfToImage = new PdfToImageViewModel(pdfToImageService, dialogService);
        ImageToPdf = new ImageToPdfViewModel(imageToPdfService, dialogService);
        MergePdf = new MergePdfViewModel(pdfMergeService, dialogService);
        SplitPdf = new SplitPdfViewModel(pdfSplitService, pdfToImageService, dialogService);
        ReorganizePdf = new ReorganizePdfViewModel(pdfReorganizeService, pdfToImageService, dialogService);

        CurrentPage = "PdfToImage";
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPdfToImageActive))]
    [NotifyPropertyChangedFor(nameof(IsImageToPdfActive))]
    [NotifyPropertyChangedFor(nameof(IsMergePdfActive))]
    [NotifyPropertyChangedFor(nameof(IsSplitPdfActive))]
    [NotifyPropertyChangedFor(nameof(IsReorganizePdfActive))]
    [NotifyPropertyChangedFor(nameof(IsAboutActive))]
    private string _currentPage = "PdfToImage";

    public bool IsPdfToImageActive => CurrentPage == "PdfToImage";
    public bool IsImageToPdfActive => CurrentPage == "ImageToPdf";
    public bool IsMergePdfActive => CurrentPage == "MergePdf";
    public bool IsSplitPdfActive => CurrentPage == "SplitPdf";
    public bool IsReorganizePdfActive => CurrentPage == "ReorganizePdf";
    public bool IsAboutActive => CurrentPage == "About";

    #endregion

    #region Relay Commands

    [RelayCommand]
    private void NavigateTo(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        CurrentPage = destination;
    }

    #endregion
}
