using AsLauncher.Core;
using AsLauncher.Models;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
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
            bool installed = ValidateVersion(versionId);

            Console.WriteLine($"IsVersionInstalled({versionId}) = {installed}");

            return installed;
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

        // HttpClient instance
        private static readonly HttpClient HttpClient = new();

        // Install process : parsing version.json and download client.jar
        public static async Task InstallVersionAsync(MinecraftVersionEntry version, CancellationToken token)
        {
            string versionFolder = Path.Combine(VersionsFolder, version.Id);

            Directory.CreateDirectory(versionFolder);

            HttpClient client = HttpClient;

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

            Task clientTask = DownloadFileWithProgressAsync(client, clientUrl, clientJarPath, token, progress =>
            {
                version.Progress = progress;
            });

            token.ThrowIfCancellationRequested();

            Task librariesTask = DownloadLibrariesAsync(version.Id, document, client, token);

            token.ThrowIfCancellationRequested();

            Task assetIndexTask = DownloadAssetIndexAsync(document, client, token);

            await Task.WhenAll(clientTask, librariesTask, assetIndexTask);

            Console.WriteLine($"Client finished for {version.Id}");
            Console.WriteLine($"Libraries finished for {version.Id}");
            Console.WriteLine($"Asset index finished for {version.Id}");

            token.ThrowIfCancellationRequested();

            string assetIndexId = document.RootElement
                                          .GetProperty("assetIndex")
                                          .GetProperty("id")
                                          .GetString()!;

            string assetIndexPath = Path.Combine(AssetsFolder, "indexes", $"{assetIndexId}.json");

            using JsonDocument assetIndexDocument = JsonDocument.Parse(await File.ReadAllTextAsync(assetIndexPath));

            await DownloadAssetsAsync(assetIndexDocument, client, token);

            Console.WriteLine($"Assets finished for {version.Id}");
        }

        // Download with progress
        private static async Task DownloadFileWithProgressAsync(
            HttpClient client,
            string url,
            string destination,
            CancellationToken token,
            Action<double>? progressCallback = null)
        {
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            await using Stream contentStream = await response.Content.ReadAsStreamAsync(token);

            await using FileStream fileStream = new(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            byte[] buffer = new byte[8192];

            long totalRead = 0;

            int read;

            while ((read = await contentStream.ReadAsync(buffer, token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), token);

                totalRead += read;

                if (totalBytes.HasValue)
                {
                    double progress = (double)totalRead / totalBytes.Value * 100;

                    progressCallback?.Invoke(progress);
                }
            }
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

        // Get inherited version ID from version.json
        public static string? GetInheritedVersion(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            if (!document.RootElement.TryGetProperty("inheritsFrom", out JsonElement inherited))
            {
                return null;
            }

            return inherited.GetString();
        }

        // Get version type from <version>.json
        public static string GetVersionType(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            if (document.RootElement.TryGetProperty("type", out JsonElement type))
            {
                return type.GetString()!;
            }

            return "release";
        }

        // Check if version is complete (version.json and version.jar exist)
        public static bool IsVersionComplete(string versionId)
        {
            return ValidateVersion(versionId);
        }

        // Check if version corrupted
        public static bool IsVersionCorrupted(string versionId)
        {
            string versionFolder = Path.Combine(VersionsFolder, versionId);

            Console.WriteLine($"Version folder exists: {Directory.Exists(versionFolder)}");

            if (!Directory.Exists(versionFolder))
                return false;

            bool valid = ValidateVersion(versionId);

            Console.WriteLine($"ValidateVersion({versionId}) = {valid}");

            return !valid;
        }

        // Check internet connection
        public static async Task<bool> HasInternetAsync()
        {
            HttpClient client = HttpClient;

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

                using HttpResponseMessage response = await HttpClient.GetAsync(
                    "https://launchermeta.mojang.com",
                    HttpCompletionOption.ResponseHeadersRead,
                    cts.Token);

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Check if any versions are installed (else 404 Not Found)
        public static bool HasInstalledVersions()
        {
            if (!Directory.Exists(VersionsFolder))
                return false;

            foreach (string directory in Directory.GetDirectories(VersionsFolder))
            {
                string versionId = Path.GetFileName(directory);

                if (IsVersionComplete(versionId))
                    return true;
            }

            return false;
        }

        // Get list of installed versions
        public static List<MinecraftVersionEntry> GetInstalledVersions()
        {
            List<MinecraftVersionEntry> versions = new();

            if (Directory.Exists(VersionsFolder))
            {
                foreach (string directory in Directory.GetDirectories(VersionsFolder))
                {
                    string versionId = Path.GetFileName(directory);
                    versions.Add(new MinecraftVersionEntry
                    {
                        Id = versionId,
                        Type = "release"
                    });
                }
            }

            if (Directory.Exists(DeletedFolder))
            {
                foreach (string directory in Directory.GetDirectories(DeletedFolder))
                {
                    string versionId = Path.GetFileName(directory)
                                           .Replace(".deleted", "");

                    versions.Add(new MinecraftVersionEntry
                    {
                        Id = versionId,
                        Type = "release",
                        InstallState = MinecraftVersionInstallState.Removed
                    });
                }
            }

            return versions;
        }

        // Load version.json
        public static JsonDocument LoadVersionJson(string versionId)
        {
            string path = GetVersionJsonPath(versionId);

            return JsonDocument.Parse(File.ReadAllText(path));
        }

        // Chunky download files
        private static async Task DownloadFileAsync(HttpClient client, string url, string destination, CancellationToken token)
        {
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

            response.EnsureSuccessStatusCode();

            await using Stream input = await response.Content.ReadAsStreamAsync(token);

            await using FileStream output = new(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            await input.CopyToAsync(output, token);
        }

        // Hash scan (SHA1)
        private static string ComputeSha1(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);

            using SHA1 sha1 = SHA1.Create();

            byte[] hash = sha1.ComputeHash(stream);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Download libraries
        private static async Task DownloadLibrariesAsync(string versionId, JsonDocument document, HttpClient client, CancellationToken token)
        {
            SemaphoreSlim semaphore = new(Theme.DownloadLibrariesLimit);

            List<Task> downloadTasks = new();

            int processedLibraries = 0;
            int downloadedLibraries = 0;
            int skippedLibraries = 0;

            JsonElement libraries = document.RootElement.GetProperty("libraries");

            int totalLibraries = libraries.EnumerateArray().Count();

            Console.WriteLine($"Libraries total: {totalLibraries}");

            foreach (JsonElement library in libraries.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();

                processedLibraries++;

                if (!library.TryGetProperty("downloads", out var downloads))
                    continue;

                if (!downloads.TryGetProperty("artifact", out var artifact))
                    continue;

                if (downloads.TryGetProperty("classifiers", out JsonElement classifiers))
                {
                    JsonElement natives;

                    bool foundNative = classifiers.TryGetProperty("natives-windows-64", out natives) ||
                                       classifiers.TryGetProperty("natives-windows", out natives);

                    if (foundNative)
                    {
                        string nativePath = natives.GetProperty("path").GetString()!;

                        string nativeUrl = natives.GetProperty("url").GetString()!;

                        string localNativeJar = Path.Combine(LibrariesFolder, nativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(localNativeJar)!);

                        if (!File.Exists(localNativeJar))
                        {
                            await DownloadFileAsync(client, nativeUrl, localNativeJar, token);
                        }

                        ExtractNatives(versionId, localNativeJar);
                    }
                }

                string path = artifact.GetProperty("path").GetString()!;

                string url = artifact.GetProperty("url").GetString()!;

                string localPath = Path.Combine(LibrariesFolder, path);

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                //string expectedSha1 = artifact.GetProperty("sha1").GetString()!;

                if (File.Exists(localPath))
                {
                    /*string actualSha1 = ComputeSha1(localPath);

                    if (actualSha1.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase))
                    {
                        skippedLibraries++;

                        continue;
                    }

                    File.Delete(localPath);*/

                    skippedLibraries++; // убрать по раскрытии SHA1
                    continue;
                }

                downloadTasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(token);

                    try
                    {
                        await DownloadFileAsync(client, url, localPath, token);

                        int current = Interlocked.Increment(ref downloadedLibraries);

                        if (current % 25 == 0)
                        {
                            Console.WriteLine($"Libraries: Downloaded {current}/{totalLibraries}");
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);

            Console.WriteLine(
                $"Libraries: Processed={processedLibraries}, " +
                $"Downloaded={downloadedLibraries}, " +
                $"Skipped={skippedLibraries}");
        }

        // Extract natives
        private static void ExtractNatives(string versionId, string nativeJarPath)
        {
            string nativesFolder = Path.Combine(VersionsFolder, versionId, "natives");

            if (!Directory.Exists(nativesFolder))
                 Directory.CreateDirectory(nativesFolder);

            ZipFile.ExtractToDirectory(nativeJarPath, nativesFolder, true);
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
            SemaphoreSlim semaphore = new(Theme.DownloadAssetsLimit);

            List<Task> downloadTasks = new();

            JsonElement objects = assetIndexDocument.RootElement.GetProperty("objects");

            int totalAssets = objects.EnumerateObject().Count();

            Console.WriteLine($"Assets total: {totalAssets}");

            int processedAssets = 0;
            int downloadedAssets = 0;
            int skippedAssets = 0;

            foreach (JsonProperty asset in objects.EnumerateObject())
            {
                downloadTasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(token);

                    try
                    {
                        token.ThrowIfCancellationRequested();

                        string hash = asset.Value.GetProperty("hash").GetString()!;

                        string folder = hash.Substring(0, 2);

                        string url = $"https://resources.download.minecraft.net/{folder}/{hash}";

                        string localFolder = Path.Combine(AssetObjectsFolder, folder);

                        Directory.CreateDirectory(localFolder);

                        string localPath = Path.Combine(localFolder, hash);

                        Interlocked.Increment(ref processedAssets);

                        if (File.Exists(localPath))
                        {
                            /*string actualSha1 = ComputeSha1(localPath);

                            if (actualSha1.Equals(hash, StringComparison.OrdinalIgnoreCase))
                            {
                                Interlocked.Increment(ref skippedAssets);

                                return;
                            }
                            
                            File.Delete(localPath);*/
                            
                            Interlocked.Increment(ref skippedAssets); // убрать по раскрытии SHA1
                            return;
                        }

                        await DownloadFileAsync(client, url, localPath, token);

                        int downloaded = Interlocked.Increment(ref downloadedAssets);

                        if (downloaded % 100 == 0)
                        {
                            Console.WriteLine(
                                $"Assets: {processedAssets}/{totalAssets}, " +
                                $"Downloaded={downloadedAssets}, " +
                                $"Skipped={skippedAssets}");
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                },

                token));
            }

            await Task.WhenAll(downloadTasks);

            Console.WriteLine(
                $"Assets: Processed={processedAssets}, " +
                $"Downloaded={downloadedAssets}, " +
                $"Skipped={skippedAssets}");
        }

        // Validate version.json and version.jar
        public static bool ValidateVersion(string versionId)
        {
            if (!File.Exists(GetVersionJsonPath(versionId)))
                return false;

            if (!File.Exists(GetVersionJarPath(versionId)))
                return false;

            using JsonDocument document = LoadVersionJson(versionId);

            if (!document.RootElement.TryGetProperty("assetIndex", out JsonElement assetIndex))
            {
                return false;
            }

            string assetIndexId = assetIndex
                .GetProperty("id")
                .GetString()!;

            string assetIndexPath = Path.Combine(
                AssetsFolder,
                "indexes",
                $"{assetIndexId}.json");

            if (!File.Exists(assetIndexPath))
            {
                return false;
            }

            using JsonDocument assetIndexDocument = JsonDocument.Parse(File.ReadAllText(assetIndexPath));

            JsonElement objects = assetIndexDocument.RootElement.GetProperty("objects");

            foreach (JsonProperty asset in objects.EnumerateObject())
            {
                string hash = asset.Value
                                   .GetProperty("hash")
                                   .GetString()!;

                string folder = hash.Substring(0, 2);

                string assetPath = Path.Combine(AssetObjectsFolder, folder, hash);

                if (!File.Exists(assetPath))
                {
                    return false;
                }
            }

            return true;
        }

        // Build classpath recursively for launch
        private static void AddLibrariesRecursive(string versionId, List<string> paths)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            if (document.RootElement.TryGetProperty("inheritsFrom", out JsonElement inherited))
            {
                string parentVersion = inherited.GetString()!;

                AddLibrariesRecursive(parentVersion, paths);
            }

            if (document.RootElement.TryGetProperty("libraries", out JsonElement libraries))
            {
                foreach (JsonElement library in libraries.EnumerateArray())
                {
                    if (!IsLibraryAllowed(library))
                        continue;

                    if (!library.TryGetProperty("downloads", out JsonElement downloads))
                        continue;

                    if (!downloads.TryGetProperty("artifact", out JsonElement artifact))
                        continue;

                    string libraryPath = artifact.GetProperty("path").GetString()!;

                    string fullPath = Path.Combine(LibrariesFolder, libraryPath);

                    if (!paths.Contains(fullPath))
                    {
                        paths.Add(fullPath);
                    }
                }
            }

            paths.Add(GetVersionJarPath(versionId));
        }

        // Build classpath for launch
        public static string BuildClassPath(string versionId)
        {
            List<string> paths = new();

            AddLibrariesRecursive(versionId, paths);

            return string.Join(Path.PathSeparator, paths);
        }

        // Check if library is allowed based on rules
        private static bool IsLibraryAllowed(JsonElement library)
        {
            if (!library.TryGetProperty("rules", out JsonElement rules))
                return true;

            bool allowed = false;

            foreach (JsonElement rule in rules.EnumerateArray())
            {
                string action = rule.GetProperty("action").GetString()!;

                bool osMatches = true;

                if (rule.TryGetProperty("os", out JsonElement os))
                {
                    if (os.TryGetProperty("name", out JsonElement name))
                    {
                        string osName = name.GetString()!;

                        osMatches = (osName == "windows" && OperatingSystem.IsWindows()) ||
                                    (osName == "linux" && OperatingSystem.IsLinux()) ||
                                    (osName == "osx" && OperatingSystem.IsMacOS());
                    }
                }

                if (!osMatches)
                    continue;

                allowed = action == "allow";
            }

            return allowed;
        }

        // Get required Java version
        public static int GetRequiredJavaVersion(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            if (document.RootElement.TryGetProperty("javaVersion", out JsonElement javaVersion))
            {
                return javaVersion.GetProperty("majorVersion")
                                  .GetInt32();
            }

            Console.WriteLine($"Version {versionId} has no javaVersion section. Using Java 8.");

            return 8;
        }

        // Get asset index id from version.json
        public static string GetAssetIndexId(string versionId)
        {
            using JsonDocument document = LoadVersionJson(versionId);

            if (!document.RootElement.TryGetProperty("assetIndex", out var assetIndex))
            {
                throw new InvalidOperationException($"Version {versionId} does not contain assetIndex.");
            }

            return assetIndex.GetProperty("id")
                             .GetString()!;
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