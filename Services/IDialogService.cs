namespace Anchor_PDF.Services;

public interface IDialogService
{
    string? ShowOpenFileDialog(string filter, string title = "Select File");
    string[]? ShowOpenFilesDialog(string filter, string title = "Select Files");
    string? ShowSaveFileDialog(string filter, string defaultExt, string title = "Save File");
    string? ShowFolderBrowserDialog(string description = "Select Folder");
    void ShowMessage(string title, string message);
    void ShowError(string title, string error);
}
