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
/// Extracts URLs+credentials from stealer log files in strict ULP format.
///
/// ONLY the following output shape is ever emitted:
///
///     &lt;host&gt;:&lt;login&gt;:&lt;password&gt;
///
/// where &lt;host&gt; MUST be one of:
///   * an http(s)/ftp/android URL (with scheme), e.g. "https://mail.corp.com/login"
///   * a bare domain with a valid TLD,          e.g. "mail.corp.com"
///   * an IPv4 host, e.g. "10.0.0.1" or "10.0.0.1:8080"
///
/// Everything else is silently discarded on purpose:
///   * bare URLs without credentials (browser history noise)
///   * email:password combos without URL (combolist noise)
///   * IP-only lines / random text / driver dumps
///   * incomplete records (url+login without password, login without password, ...)
///
/// This is what the user wants: a clean ULP file that a checker can consume,
/// with no filler entries wasting space.
/// </summary>
public static partial class LogExtractor
{
    // --- Regexes -----------------------------------------------------------

    // Strict URL regex used to find URLs embedded in free text (kept for
    // completeness; the ULP-strict mode does not emit bare URLs, but the
    // regex is still used to find URLs inside "free" lines that might look
    // like "text ... https://host:user:pass").
    [GeneratedRegex(
        @"(?:https?|ftp|android|chrome-extension|moz-extension|file)://[^\s\r\n""'<>`\[\]\\]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    // IPv4 host (with optional port and path).
    [GeneratedRegex(
        @"^(?:\d{1,3}\.){3}\d{1,3}(?::\d{1,5})?(?:/\S*)?$",
        RegexOptions.Compiled)]
    private static partial Regex Ipv4HostRegex();

    // --- Prefixes ----------------------------------------------------------

    private static readonly string[] UrlPrefixes =
    [
        "URL:", "Host:", "Hostname:", "Link:", "Site:", "Website:",
        "URL Domain:", "SoftURL:", "Origin:", "Location:"
    ];

    private static readonly string[] LoginPrefixes =
    [
        "Login:", "Username:", "User:", "Email:", "Usr:", "UserName:",
        "User Name:", "USR:", "LOGIN:", "E-mail:"
    ];

    private static readonly string[] PasswordPrefixes =
    [
        "Password:", "Pass:", "Passwd:", "PWD:", "PW:", "PASS:", "PASSWORD:"
    ];

    // File extensions to scan.
    private static readonly string[] ScanExtensions = ["*.txt", "*.log", "*.csv", "*.dat"];

    // Lines made only of separator chars (dividers between records).
    private static readonly char[] SeparatorChars = ['=', '-', '_', '*', '~', '#'];

    // Trailing junk to strip from URLs / hosts.
    private static readonly char[] UrlTrailingTrim = ['.', ',', ';', ')', ']', '}', '"', '\'', '`', '>'];

    // --- Public API --------------------------------------------------------

    /// <summary>
    /// Process all log files in the folder, extract ULP entries, save results.
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

                    foreach (var entry in ExtractFromFile(file))
                    {
                        allResults.Add(entry);
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

                var uniqueResults = allResults
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                state.ProcessedFiles = files.Count;
                state.FoundUrls = foundCount;
                state.UniqueUrls = uniqueResults.Count;
                state.CurrentFile = "";

                if (uniqueResults.Count == 0)
                {
                    state.Done = true;
                    state.Error = "No ULP entries found.";
                    onProgress(state);
                    return;
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var outputFile = Path.Combine(outputFolder, $"urls_{timestamp}.txt");

                var sb = new StringBuilder();
                sb.AppendLine("# LOGS URL Extractor | @KONDORDEVSECURITY | https://t.me/KONDORDEVSECURITY");
                sb.AppendLine($"# Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"# Files processed: {files.Count}");
                sb.AppendLine($"# ULP entries found: {foundCount}");
                sb.AppendLine($"# Unique ULP entries: {uniqueResults.Count}");
                sb.AppendLine("# Format: host:login:password  (host = http(s)/ftp URL, bare domain, or IPv4)");
                sb.AppendLine();

                foreach (var entry in uniqueResults)
                    sb.AppendLine(entry);

                File.WriteAllText(outputFile, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

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

    // --- Core extraction ---------------------------------------------------

    /// <summary>
    /// Extract ULP entries from a single file. Only host:login:password triples
    /// with a valid host are emitted; everything else is discarded.
    /// </summary>
    internal static List<string> ExtractFromFile(string filePath)
    {
        var results = new List<string>();

        string content;
        try
        {
            content = ReadFileWithBestEncoding(filePath);
        }
        catch
        {
            return results;
        }

        if (string.IsNullOrWhiteSpace(content))
            return results;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        string? currentUrl = null;
        string? currentLogin = null;
        string? currentPassword = null;

        var freeLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim().Trim('\uFEFF', '\0');
            if (trimmed.Length == 0) continue;
            if (IsSeparatorLine(trimmed)) continue;

            if (TryPrefix(trimmed, UrlPrefixes, out var urlValue))
            {
                // New URL block. Flush any complete triple we had.
                Flush(results, ref currentUrl, ref currentLogin, ref currentPassword);
                currentUrl = CleanUrl(urlValue);
                continue;
            }

            if (TryPrefix(trimmed, LoginPrefixes, out var loginValue))
            {
                if (currentLogin != null)
                    Flush(results, ref currentUrl, ref currentLogin, ref currentPassword);
                currentLogin = loginValue.Trim();
                continue;
            }

            if (TryPrefix(trimmed, PasswordPrefixes, out var passValue))
            {
                currentPassword = passValue.Trim();
                if (currentLogin != null)
                    Flush(results, ref currentUrl, ref currentLogin, ref currentPassword);
                continue;
            }

            freeLines.Add(trimmed);
        }

        Flush(results, ref currentUrl, ref currentLogin, ref currentPassword);

        // Free lines: only accept ULP-shaped combolist rows. Bare URLs,
        // email:password without URL, and any other noise are discarded.
        foreach (var line in freeLines)
        {
            var combo = TryParseUlpCombo(line);
            if (combo != null)
                results.Add(combo);
        }

        return results;
    }

    // --- Flush / format ----------------------------------------------------

    private static void Flush(
        List<string> results,
        ref string? url,
        ref string? login,
        ref string? password)
    {
        // ULP-strict: emit only when we have a complete host:login:password
        // AND the host actually looks like a URL/domain/IP.
        if (!string.IsNullOrWhiteSpace(url) &&
            !string.IsNullOrWhiteSpace(login) &&
            !string.IsNullOrWhiteSpace(password))
        {
            var host = NormalizeHost(url);
            if (host != null &&
                IsPlausibleLoginField(login) &&
                IsPlausiblePasswordField(password))
            {
                results.Add($"{host}:{login}:{password}");
            }
        }

        // Preserve the URL context; drop only credentials.
        login = null;
        password = null;
    }

    // --- Line helpers ------------------------------------------------------

    private static bool IsSeparatorLine(string line)
    {
        if (line.Length < 3) return false;
        foreach (var c in line)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (Array.IndexOf(SeparatorChars, c) < 0) return false;
        }
        return true;
    }

    private static bool TryPrefix(string line, string[] prefixes, out string value)
    {
        foreach (var prefix in prefixes)
        {
            if (line.Length > prefix.Length &&
                line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = line[prefix.Length..];
                return true;
            }
        }
        value = string.Empty;
        return false;
    }

    private static string CleanUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        var s = url.Trim().Trim('\uFEFF', '\0');

        // Strip matched surrounding wrappers iteratively.
        while (s.Length >= 2)
        {
            char first = s[0], last = s[^1];
            if ((first == '"' && last == '"') ||
                (first == '\'' && last == '\'') ||
                (first == '<' && last == '>') ||
                (first == '[' && last == ']') ||
                (first == '(' && last == ')') ||
                (first == '`' && last == '`'))
            {
                s = s.Substring(1, s.Length - 2).Trim();
                continue;
            }
            break;
        }

        s = s.TrimEnd(UrlTrailingTrim).TrimEnd();
        s = s.TrimStart('"', '\'', '<', '[', '(', '`', ' ', '\t');
        return s;
    }

    // --- Host validation ---------------------------------------------------

    /// <summary>
    /// Returns the input as a canonical "host" if it is a URL, a bare domain, or an IPv4.
    /// Returns null when the input clearly is not any of those (so we can throw it away).
    /// </summary>
    private static string? NormalizeHost(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var s = CleanUrl(raw);
        if (s.Length < 4) return null;

        // No whitespace / control chars allowed inside a host.
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c) || c < 0x21) return null;
        }

        // 1) Scheme-prefixed URL (http, https, ftp, android, ...).
        if (HasKnownScheme(s))
            return s;

        // 2) Extract the host portion (before /?#) for domain/IP checks.
        int end = s.IndexOfAny(['/', '?', '#']);
        var hostOnly = end < 0 ? s : s[..end];
        if (hostOnly.Length < 3) return null;

        // 3) IPv4.
        if (Ipv4HostRegex().IsMatch(s))
            return s;

        // 4) Bare domain: must contain a dot and end with a letter-only TLD (>= 2 chars).
        int lastDot = hostOnly.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == hostOnly.Length - 1) return null;

        var tld = hostOnly[(lastDot + 1)..];
        if (tld.Length < 2) return null;
        for (int i = 0; i < tld.Length; i++)
            if (!char.IsLetter(tld[i])) return null;

        // Reject purely numeric first label (e.g. "12345.foo" is unlikely a real host).
        var firstLabel = hostOnly[..hostOnly.IndexOf('.')];
        if (firstLabel.Length == 0) return null;
        bool allDigits = true;
        for (int i = 0; i < firstLabel.Length; i++)
            if (!char.IsDigit(firstLabel[i])) { allDigits = false; break; }
        if (allDigits) return null;

        return s;
    }

    private static bool HasKnownScheme(string s) =>
        s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("android://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("moz-extension://", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlausibleLoginField(string login)
    {
        // Reject logins with whitespace, control chars, or obvious placeholders.
        if (login.Length == 0 || login.Length > 256) return false;
        for (int i = 0; i < login.Length; i++)
        {
            char c = login[i];
            if (char.IsWhiteSpace(c) || c < 0x21) return false;
        }
        return true;
    }

    private static bool IsPlausiblePasswordField(string password)
    {
        if (password.Length == 0 || password.Length > 512) return false;
        // Leading/trailing whitespace already trimmed by caller.
        // Passwords may contain almost anything; only reject if it starts with
        // whitespace or is only whitespace/control chars.
        for (int i = 0; i < password.Length; i++)
        {
            if (password[i] > 0x20) return true;
        }
        return false;
    }

    // --- Combolist parser --------------------------------------------------

    /// <summary>
    /// Parses a free line as a ULP combo: host:login:password.
    /// Accepts host as http(s)/ftp scheme URL, bare domain with TLD, or IPv4.
    /// Returns null if the line is anything else (bare URL, email:password
    /// without URL, random text, driver dump lines, IP-only, etc.).
    /// </summary>
    private static string? TryParseUlpCombo(string line)
    {
        if (line.Length < 6) return null;
        // Reject lines with whitespace or control chars (obvious junk).
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (char.IsWhiteSpace(c) || c < 0x20) return null;
        }

        // We need at least 2 colons (one before user, one before password).
        int lastColon = line.LastIndexOf(':');
        if (lastColon <= 0 || lastColon == line.Length - 1) return null;

        int secondColon = line.LastIndexOf(':', lastColon - 1);
        if (secondColon <= 0) return null;

        var hostRaw = line[..secondColon];
        var user = line.Substring(secondColon + 1, lastColon - secondColon - 1);
        var pass = line[(lastColon + 1)..];

        if (hostRaw.Length < 3 || user.Length == 0 || pass.Length == 0) return null;
        // "//" is a URL scheme artifact, not a user.
        if (user == "/" || user == "//") return null;

        var host = NormalizeHost(hostRaw);
        if (host == null) return null;

        if (!IsPlausibleLoginField(user) || !IsPlausiblePasswordField(pass)) return null;

        return $"{host}:{user}:{pass}";
    }

    // --- Encoding detection ------------------------------------------------

    /// <summary>
    /// Reads a text file using best-effort encoding detection.
    /// Handles UTF-8/UTF-16 BOMs, BOM-less UTF-16, and falls back to Windows-1252
    /// so stealer logs from Redline / Meta / Vidar / Lumma decode without null bytes
    /// swallowing the field prefixes.
    /// </summary>
    private static string ReadFileWithBestEncoding(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0) return string.Empty;

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            return Encoding.UTF32.GetString(bytes, 4, bytes.Length - 4);

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            return new UTF32Encoding(bigEndian: true, byteOrderMark: false).GetString(bytes, 4, bytes.Length - 4);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        int sample = Math.Min(bytes.Length, 4096);
        if (sample >= 32)
        {
            int oddZeros = 0, oddTotal = 0;
            for (int i = 1; i < sample; i += 2)
            {
                oddTotal++;
                if (bytes[i] == 0) oddZeros++;
            }
            if (oddTotal > 0 && oddZeros * 100 / oddTotal >= 40)
                return Encoding.Unicode.GetString(bytes);

            int evenZeros = 0, evenTotal = 0;
            for (int i = 0; i < sample; i += 2)
            {
                evenTotal++;
                if (bytes[i] == 0) evenZeros++;
            }
            if (evenTotal > 0 && evenZeros * 100 / evenTotal >= 40)
                return Encoding.BigEndianUnicode.GetString(bytes);
        }

        try
        {
            var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return utf8Strict.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            try
            {
                return Encoding.GetEncoding(1252).GetString(bytes);
            }
            catch
            {
                return Encoding.Latin1.GetString(bytes);
            }
        }
    }
}
