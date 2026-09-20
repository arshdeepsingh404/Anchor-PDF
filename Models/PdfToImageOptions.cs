namespace Anchor_PDF.Models;

public class PdfToImageOptions
{
    public required string SourcePdfPath { get; set; }
    public required string OutputDirectory { get; set; }
    public ImageFormatType Format { get; set; } = ImageFormatType.PNG;
    public int Dpi { get; set; } = 150;
    public string PageRange { get; set; } = "All";
    public IReadOnlyList<int>? SpecificPages { get; set; }
    public bool ExportAsZip { get; set; } = false;
    public string? ZipFilePath { get; set; }
    public int Quality { get; set; } = 90;
    public List<string> GeneratedFiles { get; } = new List<string>();
}
