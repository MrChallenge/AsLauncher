using AsLauncher.Models;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class MinecraftVersionManager
    {
        // Dirs
        public static readonly string DeletedFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp", "DeletedVersions");

        public static readonly string MinecraftFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft");

        public static readonly string VersionsFolder = Path.Combine(MinecraftFolder, "Versions");

        public static readonly string LibrariesFolder = Path.Combine(MinecraftFolder, "Libraries");

        public static readonly string AssetsFolder = Path.Combine(MinecraftFolder, "Assets");

        public static readonly string AssetObjectsFolder = Path.Combine(AssetsFolder, "objects");

        // Initialize
        public static void Initialize()
        {
            Directory.CreateDirectory(DeletedFolder);

            Directory.CreateDirectory(MinecraftFolder);

            Directory.CreateDirectory(VersionsFolder);

            Directory.CreateDirectory(LibrariesFolder);

            Directory.CreateDirectory(AssetsFolder);

            Directory.CreateDirectory(AssetObjectsFolder);
        }

        // Check if version is installed
        public static bool IsVersionInstalled(string versionId)
        {
            return IsVersionComplete(versionId);
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

        // Cleanup incomplete version
        public static void CleanupIncompleteVersion(string versionId)
        {
            string versionFolder = Path.Combine(VersionsFolder, versionId);

            if (Directory.Exists(versionFolder))
            {
                Directory.Delete(versionFolder, true);
            }
        }

        // Install process : parcing version.json and download client.jar
        public static async Task InstallVersionAsync(MinecraftVersionEntry version, CancellationToken token)
        {
            string versionFolder = Path.Combine(VersionsFolder, version.Id);

            Directory.CreateDirectory(versionFolder);

            using HttpClient client = new();

            string versionJson = await client.GetStringAsync(version.Url);

            token.ThrowIfCancellationRequested();

            string versionJsonPath = Path.Combine(versionFolder, $"{version.Id}.json");

            string clientJarPath = Path.Combine(versionFolder, $"{version.Id}.jar");

            await File.WriteAllTextAsync(versionJsonPath, versionJson);

            using JsonDocument document = JsonDocument.Parse(versionJson);

            if (!document.RootElement.TryGetProperty("downloads", out var downloads))
            {
                throw new Exception($"Version {version.Id} does not contain downloads section.");
            }

            string clientUrl = document.RootElement
                                       .GetProperty("downloads")
                                       .GetProperty("client")
                                       .GetProperty("url")
                                       .GetString()!;

            byte[] clientData = await client.GetByteArrayAsync(clientUrl);

            await File.WriteAllBytesAsync(clientJarPath, clientData);

            token.ThrowIfCancellationRequested();

            await DownloadLibrariesAsync(document, client, token);

            token.ThrowIfCancellationRequested();

            Console.WriteLine($"Libraries finished for {version.Id}");

            await DownloadAssetIndexAsync(document, client, token);

            token.ThrowIfCancellationRequested();

            Console.WriteLine($"Asset index finished for {version.Id}");

            string assetIndexId = document.RootElement
                                          .GetProperty("assetIndex")
                                          .GetProperty("id")
                                          .GetString()!;

            string assetIndexPath = Path.Combine(AssetsFolder, "indexes", $"{assetIndexId}.json");

            using JsonDocument assetIndexDocument = JsonDocument.Parse(await File.ReadAllTextAsync(assetIndexPath));

            await DownloadAssetsAsync(assetIndexDocument, client, token);

            Console.WriteLine($"Assets finished for {version.Id}");
        }

        // Get version.json path
        public static string GetVersionJsonPath(string versionId)
        {
            return Path.Combine(VersionsFolder, versionId, $"{versionId}.json");
        }

        // Get version.jar path
        public static string GetVersionJarPath(string versionId)
        {
            return Path.Combine(VersionsFolder, versionId, $"{versionId}.jar");
        }

        // Check if version is complete (version.json and version.jar exist)
        public static bool IsVersionComplete(string versionId)
        {
            return

                File.Exists(GetVersionJsonPath(versionId)) && File.Exists(GetVersionJarPath(versionId));
        }

        // Check if version corrupted
        public static bool IsVersionCorrupted(string versionId)
        {
            string versionFolder = Path.Combine(VersionsFolder, versionId);

            if (!Directory.Exists(versionFolder))
                return false;

            if (!File.Exists(GetVersionJsonPath(versionId)))
                return true;

            if (!File.Exists(GetVersionJarPath(versionId)))
                return true;

            return false;
        }

        // Check internet connection
        public static bool HasInternetConnection()
        {
            try
            {
                using HttpClient client = new();

                client.Timeout = TimeSpan.FromSeconds(3);

                using HttpResponseMessage response = client.GetAsync("https://piston-meta.mojang.com",
                    HttpCompletionOption.ResponseHeadersRead).Result;

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Load version.json
        public static JsonDocument LoadVersionJson(string versionId)
        {
            string path = GetVersionJsonPath(versionId);

            return JsonDocument.Parse(File.ReadAllText(path));
        }

        // Download libraries
        private static async Task DownloadLibrariesAsync(JsonDocument document, HttpClient client, CancellationToken token)
        {
            int processedLibraries = 0;

            int downloadedLibraries = 0;

            int skippedLibraries = 0;

            JsonElement libraries = document.RootElement.GetProperty("libraries");

            foreach (JsonElement library in libraries.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();

                processedLibraries++;

                if (!library.TryGetProperty("downloads", out var downloads))
                    continue;

                if (!downloads.TryGetProperty("artifact", out var artifact))
                    continue;

                string path = artifact.GetProperty("path").GetString()!;

                string url = artifact.GetProperty("url").GetString()!;

                string localPath = Path.Combine(LibrariesFolder, path);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                if (File.Exists(localPath))
                {
                    skippedLibraries++;

                    continue;
                }

                byte[] data = await client.GetByteArrayAsync(url);

                token.ThrowIfCancellationRequested();

                await File.WriteAllBytesAsync(localPath, data);

                downloadedLibraries++;
            }

            Console.WriteLine($"Libraries: Processed={processedLibraries}, Downloaded={downloadedLibraries}, Skipped={skippedLibraries}");
        }

        // Download asset index
        private static async Task DownloadAssetIndexAsync(JsonDocument document, HttpClient client, CancellationToken token)
        {
            JsonElement assetIndex = document.RootElement.GetProperty("assetIndex");

            string assetIndexId = assetIndex.GetProperty("id").GetString()!;

            string assetIndexUrl = assetIndex.GetProperty("url").GetString()!;

            string indexesFolder = Path.Combine(AssetsFolder, "indexes");

            Directory.CreateDirectory(indexesFolder);

            string assetIndexPath = Path.Combine(indexesFolder, $"{assetIndexId}.json");

            if (File.Exists(assetIndexPath))
                return;

            string assetIndexJson = await client.GetStringAsync(assetIndexUrl);

            token.ThrowIfCancellationRequested();

            await File.WriteAllTextAsync(assetIndexPath, assetIndexJson);
        }

        // Download assets from asset index
        private static async Task DownloadAssetsAsync(JsonDocument assetIndexDocument, HttpClient client, CancellationToken token)
        {
            JsonElement objects = assetIndexDocument.RootElement.GetProperty("objects");

            int processedAssets = 0;

            int downloadedAssets = 0;

            int skippedAssets = 0;

            foreach (JsonProperty asset in objects.EnumerateObject())
            {
                token.ThrowIfCancellationRequested();

                processedAssets++;

                string hash = asset.Value.GetProperty("hash").GetString()!;

                string folder = hash.Substring(0, 2);

                string url = $"https://resources.download.minecraft.net/{folder}/{hash}";

                string localFolder = Path.Combine(AssetObjectsFolder, folder);

                Directory.CreateDirectory(localFolder);

                string localPath = Path.Combine(localFolder, hash);

                if (File.Exists(localPath))
                {
                    skippedAssets++;

                    continue;
                }

                byte[] data = await client.GetByteArrayAsync(url);
                
                token.ThrowIfCancellationRequested();

                await File.WriteAllBytesAsync(localPath, data);

                downloadedAssets++;

                if (downloadedAssets >= 50)
                    break;
            }

            Console.WriteLine($"Assets: Processed={processedAssets}, Downloaded={downloadedAssets}, Skipped={skippedAssets}");
        }

        // Build classpath for launch
        public static string BuildClassPath(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            List<string> paths = new();

            JsonElement libraries = document.RootElement.GetProperty("libraries");

            foreach (JsonElement library in libraries.EnumerateArray())
            {
                if (!library.TryGetProperty("downloads", out var downloads))
                    continue;

                if (!downloads.TryGetProperty("artifact", out var artifact))
                    continue;

                string libraryPath = artifact.GetProperty("path").GetString()!;

                paths.Add(Path.Combine(LibrariesFolder, libraryPath));
            }

            paths.Add(GetVersionJarPath(versionId));

            return string.Join(Path.PathSeparator, paths);
        }

        // Get required Java version
        public static int GetRequiredJavaVersion(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            return document.RootElement
                           .GetProperty("javaVersion")
                           .GetProperty("majorVersion")
                           .GetInt32();
        }

        // Get JVM arguments
        public static List<string> GetJvmArguments(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            List<string> arguments = new();

            JsonElement jvmArguments = document.RootElement
                                               .GetProperty("arguments")
                                               .GetProperty("jvm");

            foreach (JsonElement argument in jvmArguments.EnumerateArray())
            {
                if (argument.ValueKind == JsonValueKind.String)
                {
                    arguments.Add(argument.GetString()!);
                }
            }

            return arguments;
        }

        // Get game arguments
        public static List<string> GetGameArguments(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            List<string> arguments = new();

            JsonElement gameArguments = document.RootElement
                                                .GetProperty("arguments")
                                                .GetProperty("game");

            foreach (JsonElement argument in gameArguments.EnumerateArray())
            {
                if (argument.ValueKind == JsonValueKind.String)
                {
                    arguments.Add(argument.GetString()!);
                }
            }

            return arguments;
        }

        // Cleanup deleted versions
        public static void CleanupDeletedFolder()
        {
            if (!Directory.Exists(DeletedFolder))
                return;

            foreach (string directory in Directory.GetDirectories(DeletedFolder))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {}
            }
        }
    }
}