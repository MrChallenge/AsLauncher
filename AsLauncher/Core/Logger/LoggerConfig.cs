using System;

namespace AsLauncher.Core.Logger;

public static class LoggerConfig
{
    // Logs
    public const string LogsFolder = "logs";
    public const string LatestLog = "latest.log";

    // Logger Entry (enums)
    public const string InfoEntry = "INFO";
    public const string SuccessEntry = "SUCCESS";
    public const string WarningEntry = "WARN";
    public const string ErrorEntry = "ERROR";
    public const string DebugEntry = "DEBUG";
    public const string UnknownSource = "UNKNOWN SOURCE";

    // Time
    public const string TimeFormat = "HH:mm:ss.fff";

    // Sources
    public const string Launcher = "Launcher";
    public const string UI = "UI";

    public const string VersionsSource = "Versions";
    public const string JavaSource = "Java";
    public const string AssetsSource = "Assets";
    public const string LibrariesSource = "Libraries";
    public const string NetworkSource = "Network";
    public const string DownloaderSource = "Downloader";
    public const string CacheSource = "Cache";
    public const string ModLoaderSource = "ModLoader";

    public const string UnknownModLoaderSource = "Unknown ModLoader";

    // Console colors
    public const string InfoColor = "\x1b[38;2;255;255;255m";
    public const string SuccessColor = "\x1b[38;2;22;198;12m";
    public const string WarningColor = "\x1b[38;2;255;190;40m";
    public const string ErrorColor = "\x1b[38;2;230;52;65m";
    public const string DebugColor = "\x1b[38;2;150;150;160m";

    public const string VersionColor = "\x1b[38;2;36;99;187m";
    public const string JavaColor = "\x1b[38;2;255;135;35m";
    public const string AssetsColor = "\x1b[38;2;235;105;160m";
    public const string LibrariesColor = "\x1b[38;2;50;190;200m";
    public const string NetworkColor = "\x1b[38;2;155;110;220m";
    public const string DownloaderColor = "\x1b[38;2;70;170;255m";
    public const string CacheColor = "\x1b[38;2;125;210;95m";
    public const string ModLoaderColor = "\x1b[38;2;175;120;255m";

    public const string ResetColor = "\x1b[0m";

    // Other
    public const string LoggerWorking = "Logger is working.";
    public const string AppStarted = "App started.";
    public const string StartupCrash = "Startup Crash!";
    public const string AppClosed = "App closed.";
}