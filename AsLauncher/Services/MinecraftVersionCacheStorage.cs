using AsLauncher.Models;
using System.IO;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class MinecraftVersionCacheStorage
    {
        // Current cache version for compatibility check
        private const int CurrentCacheVersion = 1;

        // JSON serializer options for pretty printing
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        // Path to cache file in app directory
        private static readonly string CacheFilePath = AppPaths.MinecraftVersionCache;

        // Save cache to file
        public static void Save()
        {
            Directory.CreateDirectory(AppPaths.CacheDirectory);

            MinecraftVersionCacheService.Cache.CacheVersion = CurrentCacheVersion;
            MinecraftVersionCacheService.Cache.LastSync = DateTime.UtcNow;

            string json = JsonSerializer.Serialize(MinecraftVersionCacheService.Cache, JsonOptions);

            File.WriteAllText(CacheFilePath, json);
        }

        // Load cache from file, return true if successful, false otherwise
        public static bool Load()
        {
            if (!File.Exists(CacheFilePath))
                return false;

            MinecraftVersionCache? cache = JsonSerializer.Deserialize<MinecraftVersionCache>(File.ReadAllText(CacheFilePath));

            if (cache == null)
                return false;

            if (cache.CacheVersion != CurrentCacheVersion)
            {
                File.Delete(CacheFilePath);
                return false;
            }

            MinecraftVersionCacheService.ReplaceCache(cache);

            return true;
        }
    }
}