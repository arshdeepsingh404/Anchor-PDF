using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Anchor_PDF.Views;

public partial class ModernMessageDialog : Window
{
    #region Fields and Constructor

    private string? _targetPath;

    public ModernMessageDialog(string title, string message, bool isError = false)
    {
        InitializeComponent();

        TitleText.Text = title;

        if (isError)
        {
            ConfigureErrorView(message);
        }
        else
        {
            ConfigureSuccessView(message);
        }
    }

    #endregion

    #region Private Helper Methods

    private void ConfigureErrorView(string message)
    {
        SuccessBadge.Visibility = Visibility.Collapsed;
        ErrorBadge.Visibility = Visibility.Visible;
        MessageText.Text = message;
        DoneButton.Content = "Close";
        PathCard.Visibility = Visibility.Collapsed;
        OpenFolderButton.Visibility = Visibility.Collapsed;
    }

    private void ConfigureSuccessView(string message)
    {
        SuccessBadge.Visibility = Visibility.Visible;
        ErrorBadge.Visibility = Visibility.Collapsed;
        DoneButton.Content = "Done";

        (string summary, string? path) = ParseMessageAndPath(message);
        MessageText.Text = summary;
        _targetPath = path;

        if (!string.IsNullOrEmpty(_targetPath) && (File.Exists(_targetPath) || Directory.Exists(_targetPath)))
        {
            ConfigurePathCard(_targetPath);
        }
        else
        {
            PathCard.Visibility = Visibility.Collapsed;
            OpenFolderButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ConfigurePathCard(string path)
    {
        PathCard.Visibility = Visibility.Visible;
        OpenFolderButton.Visibility = Visibility.Visible;

        if (Directory.Exists(path))
        {
            FileIconContainer.Visibility = Visibility.Collapsed;
            FolderIconContainer.Visibility = Visibility.Visible;

            string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            FileNameText.Text = string.IsNullOrEmpty(folderName) ? path : folderName;
            FullPathText.Text = path;
        }
        else
        {
            FileIconContainer.Visibility = Visibility.Visible;
            FolderIconContainer.Visibility = Visibility.Collapsed;

            FileNameText.Text = Path.GetFileName(path);
            FullPathText.Text = Path.GetDirectoryName(path) ?? path;
        }
    }

    private static (string Summary, string? Path) ParseMessageAndPath(string fullMessage)
    {
        if (string.IsNullOrWhiteSpace(fullMessage))
        {
            return (string.Empty, null);
        }

        string[] lines = fullMessage.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length >= 2)
        {
            string lastLine = lines[^1].Trim();
            if (File.Exists(lastLine) || Directory.Exists(lastLine))
            {
                string summary = string.Join(" ", lines.Take(lines.Length - 1)).Trim().TrimEnd(':');
                return (summary, lastLine);
            }
        }

        return (fullMessage, null);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_targetPath))
        {
            return;
        }

        try
        {
            if (File.Exists(_targetPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_targetPath}\"",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            else if (Directory.Exists(_targetPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{_targetPath}\"",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
        }
        catch
        {
            // Ignore explorer launch issues safely
        }
    }

    #endregion
}
