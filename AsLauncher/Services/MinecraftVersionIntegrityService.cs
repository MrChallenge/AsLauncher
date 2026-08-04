using System.Windows;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using AsLauncher.Core.Logger;

namespace AsLauncher.Services
{
    public static class MinecraftVersionIntegrityService
    {
        // Hash scan (SHA1)
        public static string ComputeFileSha1(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);

            using SHA1 sha1 = SHA1.Create();

            byte[] hash = sha1.ComputeHash(stream);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Verify file integrity
        public static bool VerifyFile(string filePath, string expectedSha1)
        {
            if (!File.Exists(filePath))
                return false;

            string actualSha1 = ComputeFileSha1(filePath);

            return actualSha1.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase);
        }

        // ========================================== Validate Minecraft Version Integrity ==========================================

        // Validate assets
        public static bool ValidateAssets(string versionId)
        {
            string versionJsonPath = MinecraftPathService.GetVersionJsonPath(versionId);

            if (!File.Exists(versionJsonPath))
                return false;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(versionJsonPath));

            if (!document.RootElement.TryGetProperty("assetIndex", out JsonElement assetIndex))
                return false;

            if (!assetIndex.TryGetProperty("id", out JsonElement assetIndexIdElement))
                return false;

            string assetIndexId = assetIndexIdElement.GetString()!;

            string assetIndexPath = MinecraftPathService.GetAssetIndexPath(assetIndexId);

            if (!File.Exists(assetIndexPath))
                return false;

            using JsonDocument assetIndexDocument = JsonDocument.Parse(File.ReadAllText(assetIndexPath));

            if (!assetIndexDocument.RootElement.TryGetProperty("objects", out JsonElement objects))
                return false;

            foreach (JsonProperty asset in objects.EnumerateObject())
            {
                if (!asset.Value.TryGetProperty("hash", out JsonElement hashElement))
                    return false;

                string hash = hashElement.GetString()!;

                string assetPath = MinecraftPathService.GetAssetObjectPath(hash);

                if (!VerifyFile(assetPath, hash))
                    return false;
            }

            return true;
        }

        // Validate libraries
        public static bool ValidateLibraries(string versionId)
        {
            string versionJsonPath = MinecraftPathService.GetVersionJsonPath(versionId);

            if (!File.Exists(versionJsonPath))
                return false;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(versionJsonPath));

            if (!document.RootElement.TryGetProperty("libraries", out JsonElement libraries))
                return true;

            foreach (JsonElement library in libraries.EnumerateArray())
            {
                if (!library.TryGetProperty("downloads", out JsonElement downloads))
                    continue;

                if (!downloads.TryGetProperty("artifact", out JsonElement artifact))
                    continue;

                if (!artifact.TryGetProperty("path", out JsonElement pathElement))
                    continue;

                if (!artifact.TryGetProperty("sha1", out JsonElement sha1Element))
                    continue;

                string relativePath = pathElement.GetString()!;

                string expectedSha1 = sha1Element.GetString()!;

                string libraryPath = MinecraftPathService.GetLibraryPath(relativePath);

                if (!VerifyFile(libraryPath, expectedSha1))
                    return false;
            }

            return true;
        }

        // Validate client.jar
        public static bool ValidateClient(string versionId)
        {
            string versionJsonPath = MinecraftPathService.GetVersionJsonPath(versionId);

            string clientJarPath = MinecraftPathService.GetClientJarPath(versionId);

            if (!File.Exists(versionJsonPath))
                return false;

            if (!File.Exists(clientJarPath))
                return false;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(versionJsonPath));

            string expectedSha1 = document.RootElement
                                          .GetProperty("downloads")
                                          .GetProperty("client")
                                          .GetProperty("sha1")
                                          .GetString()!;

            return VerifyFile(clientJarPath, expectedSha1);
        }

        // Validate integrity of Minecraft version
        public static bool ValidateIntegrity(string versionId)
        {
            if (!ValidateAssets(versionId))
                return false;

            if (!ValidateLibraries(versionId))
                return false;

            if (!ValidateClient(versionId))
                return false;

            return true;
        }

        // ========================================== Ensure Minecraft version integrity ==========================================

        // Ensure assets integrity, repair if necessary
        private static async Task<bool> EnsureAssetsAsync(string versionId)
        {
            if (ValidateAssets(versionId))
                return true;

            Logger.Warning(LoggerConfig.AssetsSource, $"Detected corrupted assets for {versionId}. Starting automatic repair...");

            await MinecraftVersionManager.RepairAssetsAsync(versionId);

            if (!ValidateAssets(versionId))
            {
                Logger.Error(LoggerConfig.AssetsSource, $"Automatic asset repair failed for {versionId}.");

                MessageBox.Show("Failed to restore corrupted assets.");

                return false;
            }

            Logger.Success(LoggerConfig.AssetsSource, $"Assets restored for {versionId}. Launch continues.");

            return true;
        }

        // Ensure libraries integrity, repair if necessary
        private static async Task<bool> EnsureLibrariesAsync(string versionId)
        {
            if (ValidateLibraries(versionId))
                return true;

            Logger.Warning(LoggerConfig.LibrariesSource, $"Detected corrupted libraries for {versionId}. Starting automatic repair...");

            await MinecraftVersionManager.RepairLibrariesAsync(versionId);

            if (!ValidateLibraries(versionId))
            {
                Logger.Error(LoggerConfig.LibrariesSource, $"Automatic library repair failed for {versionId}.");

                MessageBox.Show("Failed to restore corrupted libraries.");

                return false;
            }

            Logger.Success(LoggerConfig.LibrariesSource, $"Libraries restored for {versionId}. Launch continues.");

            return true;
        }

        // Ensure client.jar integrity, repair if necessary
        private static async Task<bool> EnsureClientAsync(string versionId)
        {
            if (ValidateClient(versionId))
                return true;

            Logger.Warning(LoggerConfig.VersionsSource, $"Detected corrupted client for {versionId}. Starting automatic repair...");

            await MinecraftVersionManager.RepairClientAsync(versionId);

            if (!ValidateClient(versionId))
            {
                Logger.Error(LoggerConfig.VersionsSource, $"Automatic client repair failed for {versionId}.");

                MessageBox.Show("Failed to restore corrupted client.");

                return false;
            }

            Logger.Success(LoggerConfig.VersionsSource, $"Client restored for {versionId}. Launch continues.");

            return true;
        }

        // Ensure integrity of Minecraft version, repair if necessary
        public static async Task<bool> EnsureIntegrityAsync(string versionId)
        {
            if (!await EnsureAssetsAsync(versionId))
                return false;

            if (!await EnsureLibrariesAsync(versionId))
                return false;

            if (!await EnsureClientAsync(versionId))
                return false;

            return true;
        }
    }
}
