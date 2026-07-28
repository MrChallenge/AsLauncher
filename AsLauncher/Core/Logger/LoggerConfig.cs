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
    public const string Java = "Java";
    public const string UI = "UI";
    public const string Assets = "Assets";
    public const string Libraries = "Libraries";
    public const string Versions = "Versions";
    public const string ModLoader = "ModLoader";
    public const string UnknownModLoader = "Unknown ModLoader";
    public const string Network = "Network";
    public const string Downloader = "Downloader";
    public const string Cache = "Cache";

    // Console colors
    public const ConsoleColor InfoColor = ConsoleColor.White;
    public const ConsoleColor SuccessColor = ConsoleColor.Green;
    public const ConsoleColor WarningColor = ConsoleColor.Yellow;
    public const ConsoleColor ErrorColor = ConsoleColor.Red;
    public const ConsoleColor DebugColor = ConsoleColor.DarkGray;

    // Other
    public const string LoggerWorking = "Logger is working.";
    public const string AppStarted = "App started.";
    public const string StartupCrash = "Startup Crash!";
    public const string AppClosed = "App closed.";
}