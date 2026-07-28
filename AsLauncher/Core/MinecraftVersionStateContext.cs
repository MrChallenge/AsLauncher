namespace AsLauncher.Core
{
    public class MinecraftVersionStateContext
    {
        public required IReadOnlySet<string> InstalledVersions { get; init; }

        public bool InternetAvailable { get; init; }
    }
}