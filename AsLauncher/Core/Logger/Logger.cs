using System;
using System.IO;

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

        Directory.CreateDirectory(LoggerConfig.LogsFolder);

        string path = Path.Combine(LoggerConfig.LogsFolder,
                                   LoggerConfig.LatestLog);

        _writer = new StreamWriter(path, false)
        {
            AutoFlush = true
        };

        Info(LoggerConfig.Launcher, LoggerConfig.LoggerWorking);
    }

    public static void Info(string source, string message) => Write(LoggerEntry.Info, source, message);

    public static void Success(string source, string message) => Write(LoggerEntry.Success, source, message);

    public static void Warning(string source, string message) => Write(LoggerEntry.Warning, source, message);

    public static void Error(string source, string message) => Write(LoggerEntry.Error, source, message);

    public static void Debug(string source, string message) => Write(LoggerEntry.Debug, source, message);

    private static (string Name, ConsoleColor Color) GetEntryInfo(LoggerEntry entry)
    {
        return entry switch
        {
            LoggerEntry.Info => (LoggerConfig.InfoEntry, LoggerConfig.InfoColor),

            LoggerEntry.Success => (LoggerConfig.SuccessEntry, LoggerConfig.SuccessColor),

            LoggerEntry.Warning => (LoggerConfig.WarningEntry, LoggerConfig.WarningColor),

            LoggerEntry.Error => (LoggerConfig.ErrorEntry, LoggerConfig.ErrorColor),

            LoggerEntry.Debug => (LoggerConfig.DebugEntry, LoggerConfig.DebugColor),

            _ => (LoggerConfig.UnknownSource, ConsoleColor.White)
        };
    }

    private static void Write(LoggerEntry entry, string source, string message)
    {
        string time = DateTime.Now.ToString(LoggerConfig.TimeFormat);

        var (entryName, color) = GetEntryInfo(entry);

        message ??= string.Empty;

        string line = $"[{time}] [{entryName}] [{source}] {message}";

        lock (_lock)
        {
            // [<time>]
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(time);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");

            // [<ENTRY>]
            Console.Write("[");

            Console.ForegroundColor = color;
            Console.Write(entryName);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");

            // [<Source>]
            Console.Write("[");

            Console.ForegroundColor = color;
            Console.Write(source);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");

            // Message
            Console.ForegroundColor = color;

            Console.WriteLine(message);

            Console.ResetColor();

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
}