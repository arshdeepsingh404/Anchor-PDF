using System.IO;
using System.Windows;
using System.Windows.Threading;
using Anchor_PDF.Models;
using Anchor_PDF.Services.Implementations;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Anchor_PDF;

public partial class App : Application
{
    #region Startup and Lifetime

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterGlobalExceptionHandlers();

        if (!Environment.Is64BitOperatingSystem)
        {
            MessageBox.Show(
                "Anchor PDF is designed exclusively for 64-bit Windows systems and cannot run on a 32-bit operating system.",
                "64-Bit Windows Required",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        if (e.Args.Contains("--verify-services"))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await RunVerificationAsync();
                    Console.WriteLine("VERIFICATION_SUCCESS");
                    Dispatcher.Invoke(() => Shutdown(0));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("VERIFICATION_FAILED: " + ex.Message);
                    Dispatcher.Invoke(() => Shutdown(1));
                }
            });

            return;
        }

        Views.MainWindow mainWindow = new Views.MainWindow();
        mainWindow.Show();
    }

    #endregion

    #region Global Exception Handling

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("UI Dispatcher Exception", e.Exception);
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe operation was safely interrupted.",
            "Anchor PDF Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException("AppDomain Unhandled Exception", exception);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("Unobserved Task Exception", e.Exception);
        e.SetObserved();
    }

    private static void LogException(string source, Exception exception)
    {
        try
        {
            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnchorPDF", "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFile = Path.Combine(logDirectory, "error.log");
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}] {exception}\n\n";
            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // Suppress secondary logging failures
        }
    }

    #endregion

    #region Verification

    private static async Task RunVerificationAsync()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "AnchorPDF_Verification_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string samplePdf = Path.Combine(tempDir, "sample.pdf");
            using (PdfDocument doc = new PdfDocument())
            {
                PdfPage page1 = doc.AddPage();
                using (XGraphics gfx1 = XGraphics.FromPdfPage(page1))
                {
                    gfx1.DrawRectangle(XBrushes.SteelBlue, 50, 50, 400, 300);
                }

                PdfPage page2 = doc.AddPage();
                using (XGraphics gfx2 = XGraphics.FromPdfPage(page2))
                {
                    gfx2.DrawRectangle(XBrushes.DarkOliveGreen, 50, 50, 400, 300);
                }

                doc.Save(samplePdf);
            }

            PdfToImageService pdfService = new PdfToImageService();
            int pageCount = await pdfService.GetPageCountAsync(samplePdf);
            if (pageCount != 2)
            {
                throw new InvalidOperationException($"Expected 2 pages, got {pageCount}");
            }

            string imagesDir = Path.Combine(tempDir, "images");
            PdfToImageOptions pdfOptions = new PdfToImageOptions
            {
                SourcePdfPath = samplePdf,
                OutputDirectory = imagesDir,
                Format = ImageFormatType.PNG,
                Dpi = 150
            };
            await pdfService.ConvertPdfToImagesAsync(pdfOptions);

            string[] generatedImages = Directory.GetFiles(imagesDir, "*.png");
            if (generatedImages.Length != 2)
            {
                throw new InvalidOperationException($"Expected 2 images, found {generatedImages.Length}");
            }

            ImageToPdfService imageService = new ImageToPdfService();
            ImageItem item1 = await imageService.LoadImageItemAsync(generatedImages[0], 1);
            ImageItem item2 = await imageService.LoadImageItemAsync(generatedImages[1], 2);

            string roundtripPdf = Path.Combine(tempDir, "roundtrip.pdf");
            ImageToPdfOptions imageOptions = new ImageToPdfOptions
            {
                OutputPdfPath = roundtripPdf,
                PageSize = PageSizePreset.FitToImage
            };

            await imageService.ConvertImagesToPdfAsync([item1, item2], imageOptions);

            if (!File.Exists(roundtripPdf))
            {
                throw new FileNotFoundException("Roundtrip PDF was not created.");
            }

            int roundtripCount = await pdfService.GetPageCountAsync(roundtripPdf);
            if (roundtripCount != 2)
            {
                throw new InvalidOperationException($"Expected roundtrip PDF to have 2 pages, got {roundtripCount}");
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #endregion
}
