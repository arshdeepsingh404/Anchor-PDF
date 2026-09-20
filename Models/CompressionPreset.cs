namespace Anchor_PDF.Models;

public enum CompressionPreset
{
    Balanced,
    MaximumCompression,
    LosslessStreamOptimization
}

public class CompressionResult
{
    #region Properties

    public long OriginalSizeBytes { get; init; }
    
    public long CompressedSizeBytes { get; init; }
    
    public long SavedBytes => Math.Max(0, OriginalSizeBytes - CompressedSizeBytes);
    
    public double SavedPercentage => OriginalSizeBytes > 0 ? (double)SavedBytes / OriginalSizeBytes * 100.0 : 0;

    public string FormattedOriginalSize => FormatBytes(OriginalSizeBytes);
    
    public string FormattedCompressedSize => FormatBytes(CompressedSizeBytes);
    
    public string FormattedSavedSize => FormatBytes(SavedBytes);

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
