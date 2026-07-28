using System.IO;

namespace AsLauncher
{
    public static class AppPaths
    {
        // Root dirs of app
        public static string Root => AppContext.BaseDirectory;

        public static string CacheDirectory => Path.Combine(Root, "cache");

        public static string ConfigDirectory => Path.Combine(Root, "config");

        public static string LogsDirectory => Path.Combine(Root, "logs");

        public static string MinecraftVersionCache => Path.Combine(CacheDirectory, "minecraft_versions.json");

        public static string LauncherConfig => Path.Combine(ConfigDirectory, "launcher.json");
    }
}