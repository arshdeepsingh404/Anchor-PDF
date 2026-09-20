using System.IO;

namespace Anchor_PDF.Models;

public class MergePdfItem
{
    #region Properties

    public required string FilePath { get; init; }
    
    public string FileName => Path.GetFileName(FilePath);
    
    public int PageCount { get; set; }
    
    public long FileSizeBytes { get; init; }
    
    public int OrderIndex { get; set; }
    
    public bool IsSelected { get; set; } = true;

    public string FormattedFileSize
    {
        get
        {
            if (FileSizeBytes < 1024)
            {
                return $"{FileSizeBytes} B";
            }

            if (FileSizeBytes < 1024 * 1024)
            {
                return $"{FileSizeBytes / 1024.0:F1} KB";
            }

            return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
        }
    }

    #endregion
}
