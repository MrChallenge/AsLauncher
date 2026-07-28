using AsLauncher.Core.Logger;

public sealed class LogMessage
{
    public DateTime Time { get; init; }

    public LoggerEntry Entry { get; init; }

    public string Source { get; init; } = "";

    public string Message { get; init; } = "";
}