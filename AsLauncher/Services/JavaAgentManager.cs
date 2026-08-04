using AsLauncher.Core.Logger;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class JavaAgentManager
    {
        // Build Java Agent argument
        public static string BuildArgument(string versionId)
        {
            Logger.Debug(LoggerConfig.JavaSource, "Checking Java Agent compatibility...");

            string? javaAgentPath = FindLatestJavaAgent();

            if (javaAgentPath == null)
            {
                Logger.Debug(LoggerConfig.JavaSource, "Java Agent not found.");

                return string.Empty;
            }

            Logger.Debug(LoggerConfig.JavaSource, $"Java Agent selected: {Path.GetFileName(javaAgentPath)}");

            if (!HasJavaAgentTarget(versionId))
                return string.Empty;

            return $"-javaagent:\"{javaAgentPath}\"";
        }

        // Find latest Java Agent JAR file in app dir
        private static string? FindLatestJavaAgent()
        {
            const string agentPrefix = "AsLauncher_JavaAgent-";

            string[] javaAgentFiles = Directory.GetFiles(AppContext.BaseDirectory, $"{agentPrefix}*.jar");

            if (javaAgentFiles.Length == 0)
                return null;

            return javaAgentFiles.Select(path =>
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                string versionText = fileName[agentPrefix.Length..];

                return new
                {
                    Path = path,
                    Version = Version.TryParse(versionText, out Version? version) ? version : new Version(0, 0)
                };
            }).OrderByDescending(agent => agent.Version)
              .First()
              .Path;
        }

        // Check if Java Agent target class exists in libraries of specified version
        private static bool HasJavaAgentTarget(string versionId)
        {
            string versionJsonPath = MinecraftPathService.GetVersionJsonPath(versionId);

            if (!File.Exists(versionJsonPath))
            {
                Logger.Warning(LoggerConfig.JavaSource, $"Version JSON not found for Minecraft {versionId}.");

                return false;
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(versionJsonPath));

            if (!document.RootElement.TryGetProperty("libraries", out JsonElement libraries))
            {
                Logger.Debug(LoggerConfig.JavaSource, "No libraries found in version JSON.");

                return false;
            }

            const string targetClass = "com/mojang/authlib/yggdrasil/YggdrasilSocialInteractionsService.class";

            foreach (JsonElement library in libraries.EnumerateArray())
            {
                if (!library.TryGetProperty("downloads", out JsonElement downloads))
                    continue;

                if (!downloads.TryGetProperty("artifact", out JsonElement artifact))
                    continue;

                if (!artifact.TryGetProperty("path", out JsonElement pathElement))
                    continue;

                string? relativePath = pathElement.GetString();

                if (string.IsNullOrEmpty(relativePath))
                    continue;

                string libraryPath = MinecraftPathService.GetLibraryPath(relativePath);

                if (!File.Exists(libraryPath))
                    continue;

                try
                {
                    using ZipArchive archive = ZipFile.OpenRead(libraryPath);

                    if (archive.GetEntry(targetClass) == null)
                        continue;

                    Logger.Debug(LoggerConfig.JavaSource, $"Java Agent target detected in {Path.GetFileName(libraryPath)}.");

                    return true;
                }
                catch (InvalidDataException)
                {
                    continue;
                }
            }

            Logger.Debug(LoggerConfig.JavaSource, "Java Agent target not detected.");

            return false;
        }
    }
}