using System.IO;
using System.Windows;
using Anchor_PDF.Views;
using Microsoft.Win32;

namespace Anchor_PDF.Services.Implementations;

public class DialogService : IDialogService
{
    #region Public Methods

    public string? ShowOpenFileDialog(string filter, string title = "Select File")
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title,
            CheckFileExists = true,
            Multiselect = false,
            RestoreDirectory = true,
            InitialDirectory = GetSafeInitialDirectory()
        };

        Window? owner = Application.Current?.MainWindow;
        bool? result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }

    public string[]? ShowOpenFilesDialog(string filter, string title = "Select Files")
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title,
            CheckFileExists = true,
            Multiselect = true,
            RestoreDirectory = true,
            InitialDirectory = GetSafeInitialDirectory()
        };

        Window? owner = Application.Current?.MainWindow;
        bool? result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return result == true ? dialog.FileNames : null;
    }

    public string? ShowSaveFileDialog(string filter, string defaultExt, string title = "Save File")
    {
        SaveFileDialog dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            Title = title,
            OverwritePrompt = true,
            RestoreDirectory = true,
            InitialDirectory = GetSafeInitialDirectory()
        };

        Window? owner = Application.Current?.MainWindow;
        bool? result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }

    public string? ShowFolderBrowserDialog(string description = "Select Folder")
    {
        OpenFolderDialog dialog = new OpenFolderDialog
        {
            Title = description,
            Multiselect = false,
            InitialDirectory = GetSafeInitialDirectory()
        };

        Window? owner = Application.Current?.MainWindow;
        bool? result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        return result == true ? dialog.FolderName : null;
    }

    public void ShowMessage(string title, string message)
    {
        void DisplayDialog()
        {
            Window? owner = Application.Current?.MainWindow;
            ModernMessageDialog dialog = new ModernMessageDialog(title, message, isError: false);
            if (owner != null && owner.IsVisible && owner.WindowState != WindowState.Minimized)
            {
                dialog.Owner = owner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dialog.ShowDialog();
        }

        if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
            DisplayDialog();
        }
        else
        {
            Application.Current?.Dispatcher.Invoke(DisplayDialog);
        }
    }

    public void ShowError(string title, string error)
    {
        void DisplayDialog()
        {
            Window? owner = Application.Current?.MainWindow;
            ModernMessageDialog dialog = new ModernMessageDialog(title, error, isError: true);
            if (owner != null && owner.IsVisible && owner.WindowState != WindowState.Minimized)
            {
                dialog.Owner = owner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dialog.ShowDialog();
        }

        if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
            DisplayDialog();
        }
        else
        {
            Application.Current?.Dispatcher.Invoke(DisplayDialog);
        }
    }

    #endregion

    #region Private Helper Methods

    private static string GetSafeInitialDirectory()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (Directory.Exists(desktop))
        {
            return desktop;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    #endregion
}
