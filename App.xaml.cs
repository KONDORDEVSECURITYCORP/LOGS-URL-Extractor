using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LogsUrlExtractor.Views;

namespace LogsUrlExtractor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Catch any unhandled UI/background exception so a single-file bundle
        // failure surfaces to the user as a real error dialog + crash log,
        // instead of the process disappearing silently.
        DispatcherUnhandledException += (s, ex) =>
        {
            WriteCrashLog(ex.Exception, "DispatcherUnhandledException");
            ShowCrashDialog(ex.Exception);
            ex.Handled = true;
            Shutdown(1);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            if (ex.ExceptionObject is Exception xEx)
            {
                WriteCrashLog(xEx, "AppDomain.UnhandledException");
                ShowCrashDialog(xEx);
            }
        };

        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            WriteCrashLog(ex.Exception, "UnobservedTaskException");
            ex.SetObserved();
        };

        // Create and show the main window programmatically. Avoids the
        // StartupUri pack:// resolver which breaks in single-file bundles.
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private static void WriteCrashLog(Exception ex, string source)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogsUrlExtractor");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}");
            sb.AppendLine(ex.ToString());
            sb.AppendLine(new string('-', 80));
            File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
        }
        catch { /* nothing we can do */ }
    }

    private static void ShowCrashDialog(Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"The application encountered an unrecoverable error and will close.\n\n{ex.GetType().Name}: {ex.Message}\n\nDetails have been written to %LocalAppData%\\LogsUrlExtractor\\crash.log",
                "LOGS URL Extractor - Fatal error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { /* headless mode / cannot show UI */ }
    }
}
