using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AsLauncher.Core.Logger;

public static class Logger
{
    private static readonly object _lock = new();

    private static StreamWriter? _writer;

    public static void Initialize()
    {
        if (_writer is not null)
        {
            return;
        }

        EnableAnsiColors();

        Directory.CreateDirectory(LoggerConfig.LogsFolder);

        string path = Path.Combine(LoggerConfig.LogsFolder,
                                   LoggerConfig.LatestLog);

        _writer = new StreamWriter(path, false)
        {
            AutoFlush = true
        };

        Info(LoggerConfig.Launcher, LoggerConfig.LoggerWorking);
    }

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private static void EnableAnsiColors()
    {
        if (!OperatingSystem.IsWindows())
            return;

        IntPtr handle = GetStdHandle(StdOutputHandle);

        if (!GetConsoleMode(handle, out uint mode))
            return;

        SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    public static void Info(string source, string message) => Write(LoggerEntry.Info, source, message);

    public static void Success(string source, string message) => Write(LoggerEntry.Success, source, message);

    public static void Warning(string source, string message) => Write(LoggerEntry.Warning, source, message);

    public static void Error(string source, string message) => Write(LoggerEntry.Error, source, message);

    public static void Debug(string source, string message) => Write(LoggerEntry.Debug, source, message);

    private static (string Name, string Color) GetEntryInfo(LoggerEntry entry)
    {
        return entry switch
        {
            LoggerEntry.Info => (LoggerConfig.InfoEntry, LoggerConfig.InfoColor),

            LoggerEntry.Success => (LoggerConfig.SuccessEntry, LoggerConfig.SuccessColor),

            LoggerEntry.Warning => (LoggerConfig.WarningEntry, LoggerConfig.WarningColor),

            LoggerEntry.Error => (LoggerConfig.ErrorEntry, LoggerConfig.ErrorColor),

            LoggerEntry.Debug => (LoggerConfig.DebugEntry, LoggerConfig.DebugColor),

            _ => (LoggerConfig.UnknownSource, LoggerConfig.InfoColor)
        };
    }

    private static string GetSourceColor(string source)
    {
        return source switch
        {
            LoggerConfig.VersionsSource => LoggerConfig.VersionColor,
            LoggerConfig.JavaSource => LoggerConfig.JavaColor,
            LoggerConfig.AssetsSource => LoggerConfig.AssetsColor,
            LoggerConfig.LibrariesSource => LoggerConfig.LibrariesColor,
            LoggerConfig.NetworkSource => LoggerConfig.NetworkColor,
            LoggerConfig.DownloaderSource => LoggerConfig.DownloaderColor,
            LoggerConfig.CacheSource => LoggerConfig.CacheColor,
            LoggerConfig.ModLoaderSource => LoggerConfig.ModLoaderColor,

            _ => LoggerConfig.InfoColor
        };
    }

    private static void Write(LoggerEntry entry, string source, string message)
    {
        string time = DateTime.Now.ToString(LoggerConfig.TimeFormat);

        var (entryName, color) = GetEntryInfo(entry);

        string sourceColor = GetSourceColor(source);

        message ??= string.Empty;

        string line = $"[{time}] [{entryName}] [{source}] {message}";

        lock (_lock)
        {
            // [<time>]
            Console.Write(LoggerConfig.InfoColor);
            Console.Write("[");

            Console.Write(LoggerConfig.DebugColor);
            Console.Write(time);

            Console.Write(LoggerConfig.InfoColor);
            Console.Write("] ");

            // [<Source>]
            Console.Write("[");

            Console.Write(sourceColor);
            Console.Write(source);

            Console.Write(LoggerConfig.InfoColor);
            Console.Write("] ");

            // [<ENTRY>]
            Console.Write("[");

            Console.Write(color);
            Console.Write(entryName);

            Console.Write(LoggerConfig.InfoColor);
            Console.Write("] ");

            // Message
            Console.Write(color);
            Console.WriteLine(message);

            Console.Write(LoggerConfig.ResetColor);

            _writer?.WriteLine(line);
        }
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    // Prints current logger color palette to console
    public static void PrintColorPalette()
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine("================ AsLauncher Logger Color Palette ================");
            Console.WriteLine();

            PrintColor(LoggerConfig.InfoEntry, LoggerConfig.InfoColor);
            PrintColor(LoggerConfig.SuccessEntry, LoggerConfig.SuccessColor);
            PrintColor(LoggerConfig.WarningEntry, LoggerConfig.WarningColor);
            PrintColor(LoggerConfig.ErrorEntry, LoggerConfig.ErrorColor);
            PrintColor(LoggerConfig.DebugEntry, LoggerConfig.DebugColor);

            Console.WriteLine();

            PrintColor(LoggerConfig.VersionsSource, LoggerConfig.VersionColor);
            PrintColor(LoggerConfig.JavaSource, LoggerConfig.JavaColor);
            PrintColor(LoggerConfig.AssetsSource, LoggerConfig.AssetsColor);
            PrintColor(LoggerConfig.LibrariesSource, LoggerConfig.LibrariesColor);
            PrintColor(LoggerConfig.NetworkSource, LoggerConfig.NetworkColor);
            PrintColor(LoggerConfig.DownloaderSource, LoggerConfig.DownloaderColor);
            PrintColor(LoggerConfig.CacheSource, LoggerConfig.CacheColor);
            PrintColor(LoggerConfig.ModLoaderSource, LoggerConfig.ModLoaderColor);

            Console.WriteLine();
            Console.WriteLine("=================================================================");
            Console.WriteLine();
        }
    }

    private static void PrintColor(string name, string color)
    {
        // Color sample
        Console.Write(color);
        Console.Write("███");

        Console.Write(LoggerConfig.ResetColor);
        Console.Write("  ");

        // [<SOURCE>]
        Console.Write(LoggerConfig.ResetColor);
        Console.Write("[");

        Console.Write(color);
        Console.Write(name);

        Console.Write(LoggerConfig.ResetColor);
        Console.Write("] ");

        // Message
        Console.Write(color);
        Console.WriteLine("Something broke again. I'm too lazy to fix it.");

        Console.Write(LoggerConfig.ResetColor);
    }
}