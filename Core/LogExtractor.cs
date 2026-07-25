using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogsUrlExtractor.Core;

/// <summary>
/// Processing state reported to the UI.
/// </summary>
public class ExtractionState
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FoundUrls { get; set; }
    public int UniqueUrls { get; set; }
    public string CurrentFile { get; set; } = "";
    public string? Error { get; set; }
    public bool Done { get; set; }
    public string? SavedTo { get; set; }
}

/// <summary>
/// Extracts URLs and credentials from stealer log files.
/// Supports parallel processing, deduplication, and multiple log formats.
/// </summary>
public static partial class LogExtractor
{
    // Regex for standalone URLs
    [GeneratedRegex(@"(https?://[^\s\r\n""'<>]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    // Stealer log prefixes
    private static readonly string[] UrlPrefixes = ["URL:", "Host:", "Hostname:", "Link:", "Site:"];
    private static readonly string[] LoginPrefixes = ["Login:", "Username:", "User:", "Email:", "LOGIN:"];
    private static readonly string[] PasswordPrefixes = ["Password:", "Pass:", "PASS:", "Passwd:"];

    // File extensions to scan
    private static readonly string[] ScanExtensions = ["*.txt", "*.log", "*.csv", "*.dat"];

    /// <summary>
    /// Process all log files in the folder, extract URLs/credentials, save results.
    /// </summary>
    public static Task ProcessFolderAsync(
        string inputFolder,
        string outputFolder,
        Action<ExtractionState> onProgress,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var state = new ExtractionState();

            try
            {
                Directory.CreateDirectory(outputFolder);

                // Scan for files
                var files = new List<string>();
                foreach (var ext in ScanExtensions)
                {
                    try
                    {
                        files.AddRange(Directory.GetFiles(inputFolder, ext, SearchOption.AllDirectories));
                    }
                    catch { }
                }

                state.TotalFiles = files.Count;
                onProgress(state);

                if (files.Count == 0)
                {
                    state.Done = true;
                    state.Error = "No log files found in the selected folder.";
                    onProgress(state);
                    return;
                }

                // Process in parallel
                var allResults = new ConcurrentBag<string>();
                int processedCount = 0;
                int foundCount = 0;

                Parallel.ForEach(files, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = ct
                },
                file =>
                {
                    state.CurrentFile = Path.GetFileName(file);

                    var extracted = ExtractFromFile(file);
                    foreach (var line in extracted)
                    {
                        allResults.Add(line);
                        Interlocked.Increment(ref foundCount);
                    }

                    var processed = Interlocked.Increment(ref processedCount);

                    if (processed % 30 == 0 || processed == files.Count)
                    {
                        state.ProcessedFiles = processed;
                        state.FoundUrls = foundCount;
                        onProgress(state);
                    }
                });

                ct.ThrowIfCancellationRequested();

                // Deduplicate and save
                var uniqueResults = allResults.Distinct().OrderBy(x => x).ToList();
                state.ProcessedFiles = files.Count;
                state.FoundUrls = foundCount;
                state.UniqueUrls = uniqueResults.Count;
                state.CurrentFile = "";

                if (uniqueResults.Count == 0)
                {
                    state.Done = true;
                    state.Error = "No URLs or credentials found.";
                    onProgress(state);
                    return;
                }

                // Save results
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var outputFile = Path.Combine(outputFolder, $"urls_{timestamp}.txt");

                var sb = new StringBuilder();
                sb.AppendLine($"# LOGS URL Extractor | @KONDORDEVSECURITY | https://t.me/KONDORDEVSECURITY");
                sb.AppendLine($"# Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"# Files processed: {files.Count}");
                sb.AppendLine($"# URLs found: {foundCount}");
                sb.AppendLine($"# Unique entries: {uniqueResults.Count}");
                sb.AppendLine();

                foreach (var entry in uniqueResults)
                    sb.AppendLine(entry);

                File.WriteAllText(outputFile, sb.ToString(), Encoding.UTF8);

                state.Done = true;
                state.SavedTo = outputFile;
                onProgress(state);
            }
            catch (OperationCanceledException)
            {
                state.Done = true;
                state.Error = "Operation cancelled.";
                onProgress(state);
            }
            catch (Exception ex)
            {
                state.Done = true;
                state.Error = $"Error: {ex.Message}";
                onProgress(state);
            }
        }, ct);
    }

    /// <summary>
    /// Extract URLs and credentials from a single file.
    /// </summary>
    private static List<string> ExtractFromFile(string filePath)
    {
        var results = new List<string>();

        try
        {
            var content = File.ReadAllText(filePath);
            var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            string? currentUrl = null;
            string? currentLogin = null;
            string? currentPassword = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Check URL prefixes
                foreach (var prefix in UrlPrefixes)
                {
                    if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentUrl != null)
                            results.Add(FormatEntry(currentUrl, currentLogin, currentPassword));

                        currentUrl = trimmed[prefix.Length..].Trim();
                        currentLogin = null;
                        currentPassword = null;
                        break;
                    }
                }

                // Check login prefixes
                foreach (var prefix in LoginPrefixes)
                {
                    if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        currentLogin = trimmed[prefix.Length..].Trim();
                        break;
                    }
                }

                // Check password prefixes
                foreach (var prefix in PasswordPrefixes)
                {
                    if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        currentPassword = trimmed[prefix.Length..].Trim();
                        break;
                    }
                }
            }

            // Last entry
            if (currentUrl != null)
                results.Add(FormatEntry(currentUrl, currentLogin, currentPassword));

            // Standalone URLs via regex
            foreach (Match match in UrlRegex().Matches(content))
            {
                var url = match.Value.TrimEnd('.', ',', ';', ')', ']', '}', '"', '\'');
                if (url.Length > 10 && !results.Any(r => r.Contains(url)))
                    results.Add(url);
            }
        }
        catch { }

        return results;
    }

    private static string FormatEntry(string url, string? login, string? password)
    {
        if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
            return $"{url}:{login}:{password}";
        if (!string.IsNullOrEmpty(login))
            return $"{url}:{login}";
        return url;
    }
}
