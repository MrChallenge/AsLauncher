using AsLauncher.Models;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class MinecraftVersionManager
    {
        // Dirs
        public static readonly string MinecraftFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft");

        public static readonly string VersionsFolder = Path.Combine(MinecraftFolder, "Versions");

        public static readonly string LibrariesFolder = Path.Combine(MinecraftFolder, "Libraries");

        public static readonly string AssetsFolder = Path.Combine(MinecraftFolder, "Assets");

        public static readonly string DeletedFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp", "DeletedVersions");

        // Initialize
        public static void Initialize()
        {
            Directory.CreateDirectory(MinecraftFolder);

            Directory.CreateDirectory(VersionsFolder);

            Directory.CreateDirectory(LibrariesFolder);

            Directory.CreateDirectory(AssetsFolder);

            Directory.CreateDirectory(DeletedFolder);
        }

        // Check if version is installed
        public static bool IsVersionInstalled(string versionId)
        {
            return Directory.Exists(Path.Combine(VersionsFolder, versionId));
        }

        // Check if version is deleted
        public static bool IsVersionDeleted(string versionId)
        {
            string deletedPath = Path.Combine(DeletedFolder, versionId + ".deleted");

            return Directory.Exists(deletedPath);
        }

        // Delete version (move to deleted folder)
        public static bool DeleteVersion(string versionId)
        {
            string versionPath = Path.Combine(VersionsFolder, versionId);

            if (!Directory.Exists(versionPath))
                return false;

            string deletedPath = Path.Combine(DeletedFolder, versionId + ".deleted");

            if (Directory.Exists(deletedPath))
            {
                Directory.Delete(deletedPath, true);
            }

            Directory.Move(versionPath, deletedPath);

            return true;
        }

        // Restore version (move from deleted folder)
        public static bool RestoreVersion(string versionId)
        {
            string deletedPath = Path.Combine(DeletedFolder, versionId + ".deleted");

            if (!Directory.Exists(deletedPath))
                return false;

            string versionPath = Path.Combine(VersionsFolder, versionId);

            if (Directory.Exists(versionPath))
            {
                Directory.Delete(versionPath, true);
            }

            Directory.Move(deletedPath, versionPath);

            return true;
        }

        // Install process : parcing version.json and download client.jar
        public static async Task InstallVersionAsync(MinecraftVersionEntry version)
        {
            string versionFolder = Path.Combine(VersionsFolder, version.Id);

            Directory.CreateDirectory(versionFolder);

            using HttpClient client = new();

            string versionJson = await client.GetStringAsync(version.Url);

            string versionJsonPath = Path.Combine(versionFolder, $"{version.Id}.json");

            string clientJarPath = Path.Combine(versionFolder, $"{version.Id}.jar");

            await File.WriteAllTextAsync(versionJsonPath, versionJson);

            using JsonDocument document = JsonDocument.Parse(versionJson);

            string clientUrl = document.RootElement
                                       .GetProperty("downloads")
                                       .GetProperty("client")
                                       .GetProperty("url")
                                       .GetString()!;

            if (!document.RootElement.TryGetProperty("downloads", out var downloads))
            {
                throw new Exception($"Version {version.Id} does not contain downloads section.");
            }

            byte[] clientData = await client.GetByteArrayAsync(clientUrl);

            await File.WriteAllBytesAsync(clientJarPath, clientData);
        }
    }
}