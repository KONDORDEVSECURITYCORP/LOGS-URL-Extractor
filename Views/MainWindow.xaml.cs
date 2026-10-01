using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using LogsUrlExtractor.Core;

namespace LogsUrlExtractor.Views;

public partial class MainWindow : Window
{
    private string _inputFolder = "";
    private string _outputFolder = "";
    private CancellationTokenSource? _cts;
    private byte[]? _embeddedHtmlBytes;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogsUrlExtractor");
        Directory.CreateDirectory(appData);
        var userDataFolder = Path.Combine(appData, "WebView2");

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await WebView.EnsureCoreWebView2Async(env);

        WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

        WebView.CoreWebView2.WebMessageReceived += OnWebMessage;

        // Load embedded HTML
        var html = LoadEmbeddedHtml();
        if (html != null)
        {
            _embeddedHtmlBytes = Encoding.UTF8.GetBytes(html);
            WebView.CoreWebView2.AddWebResourceRequestedFilter("https://app.local/*", CoreWebView2WebResourceContext.All);
            WebView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
            WebView.CoreWebView2.Navigate("https://app.local/index.html");
        }
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (_embeddedHtmlBytes != null)
        {
            var stream = new MemoryStream(_embeddedHtmlBytes);
            e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                stream, 200, "OK", "Content-Type: text/html; charset=utf-8\nCache-Control: no-cache");
        }
    }

    private static string? LoadEmbeddedHtml()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("index.html");
        if (stream == null) return null;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.TryGetWebMessageAsString();
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var cmd = doc.RootElement.GetProperty("cmd").GetString() ?? "";
            Dispatcher.Invoke(() => HandleCommand(cmd));
        }
        catch { }
    }

    private void HandleCommand(string cmd)
    {
        switch (cmd)
        {
            case "browseInput":
                BrowseInput();
                break;
            case "browseOutput":
                BrowseOutput();
                break;
            case "start":
                StartExtraction();
                break;
            case "stop":
                _cts?.Cancel();
                break;
            case "openLink":
                try { Process.Start(new ProcessStartInfo { FileName = "https://t.me/KONDORDEVSECURITY", UseShellExecute = true }); } catch { }
                break;
            case "minimize":
                WindowState = WindowState.Minimized;
                break;
            case "maximize":
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                break;
            case "close":
                Close();
                break;
        }
    }

    private void BrowseInput()
    {
        var dialog = new OpenFolderDialog { Title = "Select folder with logs" };
        if (dialog.ShowDialog() == true)
        {
            _inputFolder = dialog.FolderName;
            JS($"setInputPath({JsonStr(_inputFolder)})");

            if (string.IsNullOrEmpty(_outputFolder))
            {
                _outputFolder = Path.Combine(_inputFolder, "extracted_urls");
                JS($"setOutputPath({JsonStr(_outputFolder)})");
            }
        }
    }

    private void BrowseOutput()
    {
        var dialog = new OpenFolderDialog { Title = "Select output folder" };
        if (dialog.ShowDialog() == true)
        {
            _outputFolder = dialog.FolderName;
            JS($"setOutputPath({JsonStr(_outputFolder)})");
        }
    }

    private async void StartExtraction()
    {
        if (string.IsNullOrEmpty(_inputFolder) || !Directory.Exists(_inputFolder))
        {
            JS("setError('Please select a valid input folder.')");
            return;
        }

        if (string.IsNullOrEmpty(_outputFolder))
            _outputFolder = Path.Combine(_inputFolder, "extracted_urls");

        JS("setRunning(true)");
        JS("setStatus('Scanning files...')");

        _cts = new CancellationTokenSource();

        try
        {
            await LogExtractor.ProcessFolderAsync(
                _inputFolder,
                _outputFolder,
                state =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        JS($"updateStats({{total:{state.TotalFiles},processed:{state.ProcessedFiles},found:{state.FoundUrls},unique:{state.UniqueUrls},currentFile:{JsonStr(state.CurrentFile)}}})");

                        if (state.Done)
                        {
                            if (state.SavedTo != null)
                                JS($"setDone({JsonStr(state.SavedTo)})");
                            else if (state.Error != null)
                                JS($"setError({JsonStr(state.Error)})");
                        }
                    });
                },
                _cts.Token);
        }
        catch (Exception ex)
        {
            JS($"setError({JsonStr(ex.Message)})");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void JS(string script)
    {
        WebView.CoreWebView2?.ExecuteScriptAsync(script);
    }

    private static string JsonStr(string s)
    {
        return JsonSerializer.Serialize(s);
    }

    // Allow dragging the window from the titlebar area
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
    }
}
