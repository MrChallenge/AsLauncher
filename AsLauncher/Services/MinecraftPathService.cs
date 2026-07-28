using System.IO;
using static AsLauncher.Services.MinecraftVersionManager;

namespace AsLauncher.Services
{
    public static class MinecraftPathService
    {
        public static string GetVersionFolder(string versionId) => Path.Combine(VersionsFolder, versionId);

        public static string GetVersionJsonPath(string versionId) => Path.Combine(GetVersionFolder(versionId), $"{versionId}.json");

        public static string GetClientJarPath(string versionId) => Path.Combine(GetVersionFolder(versionId), $"{versionId}.jar");

        public static string GetAssetIndexPath(string assetIndexId) => Path.Combine(AssetsFolder, "indexes", $"{assetIndexId}.json");

        public static string GetLibraryPath(string relativePath) => Path.Combine(LibrariesFolder, relativePath);

        public static string GetAssetObjectFolder(string hash) => Path.Combine(AssetsFolder, "objects", hash[..2]);

        public static string GetAssetObjectUrl(string hash)
        {
            return $"https://resources.download.minecraft.net/{hash[..2]}/{hash}";
        }

        public static string GetAssetObjectPath(string hash) => Path.Combine(GetAssetObjectFolder(hash), hash);
    }
}
