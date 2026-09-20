namespace Anchor_PDF.Models;

public class ImageToPdfOptions
{
    public required string OutputPdfPath { get; set; }
    public PageSizePreset PageSize { get; set; } = PageSizePreset.FitToImage;
    public PageOrientationPreset Orientation { get; set; } = PageOrientationPreset.Auto;
    public double MarginPoints { get; set; } = 0;
    public int Quality { get; set; } = 90;
}
