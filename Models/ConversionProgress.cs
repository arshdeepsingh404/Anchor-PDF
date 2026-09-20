namespace Anchor_PDF.Models;

public record ConversionProgress(
    double Percentage,
    int CurrentItem,
    int TotalItems,
    string Message,
    bool IsIndeterminate = false
);
