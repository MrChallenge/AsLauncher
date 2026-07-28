using AsLauncher.Core;
using AsLauncher.Core.Logger;
using AsLauncher.Models;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

using Lang = AsLauncher.Resources.Localization.Resources;

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

        // Initialization flag
        private static bool _initialized;

        // Initialize async (load cache and sync versions)
        public static async Task InitializeAsync()
        {
            if (_initialized)
                return;

            MinecraftVersionCacheStorage.Load();

            await MinecraftVersionSyncService.SyncAsync();

            _initialized = true;
        }

        // Check if version dir exists
        public static bool VersionDirectoryExists(string versionId)
        {
            return Directory.Exists(Path.Combine(VersionsFolder, versionId));
        }

        // Check if version is installed
        public static bool IsVersionInstalled(string versionId)
        {
            return ValidateVersion(versionId);
        }

        // Get installed versions IDs
        public static HashSet<string> GetInstalledVersionIds()
        {
            if (!Directory.Exists(VersionsFolder))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return Directory.EnumerateDirectories(VersionsFolder)
                            .Select(Path.GetFileName)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Cast<string>()
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // Create state context for version install states
        public static MinecraftVersionStateContext CreateStateContext(bool internetAvailable)
        {
            return new MinecraftVersionStateContext
            {
                InstalledVersions = GetInstalledVersionIds(),
                InternetAvailable = internetAvailable
            };
        }

        // Check if version is deleted
        public static bool IsVersionDeleted(string versionId)
        {
            string deletedPath = Path.Combine(DeletedFolder, versionId + ".deleted");

            return Directory.Exists(deletedPath);
        }

        // Get version install state
        public static MinecraftVersionInstallState GetVersionState(string versionId, MinecraftVersionStateContext context)
        {
            if (IsVersionDeleted(versionId))
                return MinecraftVersionInstallState.Removed;

            if (!context.InstalledVersions.Contains(versionId))
            {
                return context.InternetAvailable
                    ? MinecraftVersionInstallState.NotInstalled
                    : MinecraftVersionInstallState.Unavailable;
            }

            return ValidateVersionFast(versionId)
                ? MinecraftVersionInstallState.Installed
                : MinecraftVersionInstallState.Corrupted;
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
            Logger.Info(LoggerConfig.Versions, $"Installing Minecraft {version.Id}...");

            string versionFolder = MinecraftPathService.GetVersionFolder(version.Id);

            Directory.CreateDirectory(versionFolder);

            HttpClient client = HttpClient;

            string versionJson = await client.GetStringAsync(version.Url);

            token.ThrowIfCancellationRequested();

            string versionJsonPath = MinecraftPathService.GetVersionJsonPath(version.Id);

            string clientJarPath = MinecraftPathService.GetClientJarPath(version.Id);

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

            Task clientTask = DownloadFileWithProgressAsync(client, clientUrl, clientJarPath, token, downloadProgress =>
            {
                version.DownloadProgress = downloadProgress;
            });

            token.ThrowIfCancellationRequested();

            Task librariesTask = DownloadLibrariesAsync(version.Id, document, client, token);

            token.ThrowIfCancellationRequested();

            Task assetIndexTask = DownloadAssetIndexAsync(document, client, token);

            await Task.WhenAll(clientTask, librariesTask, assetIndexTask);

            Logger.Success(LoggerConfig.Versions, $"Version {version.Id} downloaded successfully.");

            token.ThrowIfCancellationRequested();

            string assetIndexId = document.RootElement
                                          .GetProperty("assetIndex")
                                          .GetProperty("id")
                                          .GetString()!;

            string assetIndexPath = MinecraftPathService.GetAssetIndexPath(assetIndexId);

            using JsonDocument assetIndexDocument = JsonDocument.Parse(await File.ReadAllTextAsync(assetIndexPath));

            Logger.Info(LoggerConfig.Assets, $"Downloading assets for {version.Id}...");

            await DownloadAssetsAsync(assetIndexDocument, client, token);

            Logger.Success(LoggerConfig.Assets, $"Assets downloaded for {version.Id}.");
        }

        // Download with progress
        private static async Task DownloadFileWithProgressAsync(
            HttpClient client,
            string url,
            string destination,
            CancellationToken token,
            Action<double>? downloadProgressCallback = null)
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
                    double downloadProgress = (double)totalRead / totalBytes.Value * 100;

                    downloadProgressCallback?.Invoke(downloadProgress);
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

            if (!Directory.Exists(versionFolder))
            {
                return false;
            }

            bool valid = ValidateVersion(versionId);

            Logger.Debug(LoggerConfig.Versions, $"Version validation ({versionId}): {(valid ? "Valid" : "Corrupted")}");

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
                        Type = MinecraftVersionType.Release
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
                        Type = MinecraftVersionType.Release,
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

        // Validate versions quickly (page refresh)
        public static bool ValidateVersionFast(string versionId)
        {
            string versionFolder = Path.Combine(VersionsFolder, versionId);

            if (!Directory.Exists(versionFolder))
                return false;

            if (!File.Exists(GetVersionJsonPath(versionId)))
                return false;

            if (!File.Exists(GetVersionJarPath(versionId)))
                return false;

            return true;
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

            Logger.Info(LoggerConfig.Libraries, $"Downloading {totalLibraries} libraries for {versionId}...");

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

                        Logger.Debug(LoggerConfig.Libraries, $"Extracting {Path.GetFileName(localNativeJar)}...");

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

                string expectedSha1 = artifact.GetProperty("sha1").GetString()!;

                if (MinecraftVersionIntegrityService.VerifyFile(localPath, expectedSha1))
                {
                    skippedLibraries++;

                    continue;
                }

                if (File.Exists(localPath))
                {
                    Logger.Warning(LoggerConfig.Libraries, $"SHA1 mismatch: {Path.GetFileName(localPath)}. Redownloading.");

                    File.Delete(localPath);
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
                            Logger.Debug(LoggerConfig.Libraries, $"Downloaded {current}/{totalLibraries} libraries.");
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);

            Logger.Success(LoggerConfig.Libraries, $"Processed={processedLibraries}," +
                                                   $"Downloaded={downloadedLibraries}," +
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

            string assetIndexId = assetIndex.GetProperty("id")
                                            .GetString()!;

            string assetIndexUrl = assetIndex.GetProperty("url")
                                             .GetString()!;

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

            Logger.Info(LoggerConfig.Assets, $"Assets total: {totalAssets}");

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

                        string url = MinecraftPathService.GetAssetObjectUrl(hash);

                        string localFolder = MinecraftPathService.GetAssetObjectFolder(hash);

                        Directory.CreateDirectory(localFolder);

                        string localPath = MinecraftPathService.GetAssetObjectPath(hash);

                        Interlocked.Increment(ref processedAssets);

                        if (MinecraftVersionIntegrityService.VerifyFile(localPath, hash))
                        {
                            Interlocked.Increment(ref skippedAssets);

                            return;
                        }

                        if (File.Exists(localPath))
                        {
                            Logger.Warning(LoggerConfig.Assets, $"SHA1 mismatch: {hash}. Redownloading.");

                            File.Delete(localPath);
                        }

                        await DownloadFileAsync(client, url, localPath, token);

                        int downloaded = Interlocked.Increment(ref downloadedAssets);

                        if (downloaded % 100 == 0)
                        {
                            Logger.Debug(LoggerConfig.Assets, $"Assets: {processedAssets}/{totalAssets}," +
                                                              $"Downloaded={downloadedAssets}," +
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

            Logger.Success(LoggerConfig.Assets, $"Processed={processedAssets}," +
                                                $"Downloaded={downloadedAssets}," +
                                                $"Skipped={skippedAssets}");
        }

        // Validate all required version files and assets
        public static bool ValidateVersion(string versionId,
            Action<double>? setupProgressCallback = null,
            Action<string>? setupStatusCallback = null)
        {
            // Progress stages
            const double ProgressStart = 0;
            const double ProgressVersionJson = 10;
            const double ProgressClientJar = 20;
            const double ProgressVersionJsonRead = 30;
            const double ProgressAssetIndex = 40;
            const double ProgressAssetIndexFile = 50;
            const double ProgressAssetIndexRead = 60;
            const double ProgressAssets = 70;
            const double ProgressFinish = 100;

            // Report current validation stage
            void ReportProgress(double progress, string status)
            {
                setupStatusCallback?.Invoke(status);
                setupProgressCallback?.Invoke(progress);

                Thread.Sleep(Theme.ForcedDelay);
            }

            // Start
            ReportProgress(ProgressStart, Lang.Progress_Start);

            // Check version.json
            ReportProgress(ProgressVersionJson, Lang.Progress_VersionJson);

            if (!File.Exists(GetVersionJsonPath(versionId)))
                return false;

            // Check version.jar
            ReportProgress(ProgressClientJar, Lang.Progress_ClientJar);

            if (!File.Exists(GetVersionJarPath(versionId)))
                return false;

            // Read version.json
            ReportProgress(ProgressVersionJsonRead, Lang.Progress_VersionJsonRead);

            using JsonDocument document = LoadVersionJson(versionId);

            if (!document.RootElement.TryGetProperty("assetIndex", out JsonElement assetIndex))
                return false;

            // Read asset index information
            ReportProgress(ProgressAssetIndex, Lang.Progress_AssetIndex);

            string assetIndexId = assetIndex.GetProperty("id")
                                            .GetString()!;

            string assetIndexPath = Path.Combine(AssetsFolder, "indexes", $"{assetIndexId}.json");

            // Check asset index file
            ReportProgress(ProgressAssetIndexFile, Lang.Progress_AssetIndexFile);

            if (!File.Exists(assetIndexPath))
                return false;

            // Read asset index
            ReportProgress(ProgressAssetIndexRead, Lang.Progress_AssetIndexRead);

            using JsonDocument assetIndexDocument = JsonDocument.Parse(File.ReadAllText(assetIndexPath));

            JsonElement objects = assetIndexDocument.RootElement.GetProperty("objects");

            JsonProperty[] assets = objects.EnumerateObject().ToArray();

            int totalAssets = assets.Length;

            if (totalAssets == 0)
            {
                ReportProgress(ProgressFinish, Lang.Progress_Finish);

                return true;
            }

            int checkedAssets = 0;

            ReportProgress(ProgressAssets, Lang.Progress_Assets);

            foreach (JsonProperty asset in assets)
            {
                checkedAssets++;

                string hash = asset.Value.GetProperty("hash")
                                         .GetString()!;

                string folder = hash.Substring(0, 2);

                string assetPath = Path.Combine(AssetObjectsFolder, folder, hash);

                if (!File.Exists(assetPath))
                {
                    return false;
                }

                // Update progress while validating asset files
                double progress = ProgressAssets + checkedAssets * (ProgressFinish - ProgressAssets) / totalAssets;

                setupProgressCallback?.Invoke(progress);
            }

            // Finish
            ReportProgress(ProgressFinish, Lang.Progress_Finish);

            return true;
        }

        // Validate version async
        public static Task<bool> ValidateVersionAsync(string versionId,
            Action<double>? progress,
            Action<string>? status)
        {
            return Task.Run(() => ValidateVersion(versionId, progress, status));
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

            Logger.Info(LoggerConfig.Java, $"Version {versionId} does not specify Java version. Using Java 8.");

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