using AsLauncher.Models;

namespace AsLauncher.Services
{
    public static class MinecraftVersionCacheService
    {
        // Current cache instance
        public static MinecraftVersionCache Cache { get; private set; } = new();

        // Get total count of versions in lookup dictionary
        public static int Count => Cache.Lookup.Count;

        // Clear cache and lookup dictionary
        public static void Clear()
        {
            Cache = new();
        }

        // Get versions of specific type from cache
        public static IEnumerable<MinecraftVersionInfo> GetVersions(MinecraftVersionType type)
        {
            return GetCollection(type);
        }

        // Check if version exists in lookup dictionary
        public static bool Contains(string id) => Cache.Lookup.ContainsKey(id);

        // Find version by ID in lookup dictionary
        public static MinecraftVersionInfo? Find(string id)
        {
            Cache.Lookup.TryGetValue(id, out MinecraftVersionInfo? version);

            return version;
        }

        // Add version to cache and lookup
        public static void AddVersion(MinecraftVersionInfo version)
        {
            if (Cache.Lookup.ContainsKey(version.Id))
                return;

            GetCollection(version.Type).Add(version);

            Cache.Lookup[version.Id] = version;
        }

        // Add multiple versions to cache and lookup
        public static void AddVersions(IEnumerable<MinecraftVersionInfo> versions)
        {
            foreach (MinecraftVersionInfo version in versions)
            {
                AddVersion(version);
            }
        }

        // Remove version from cache and lookup
        public static bool RemoveVersion(string id)
        {
            MinecraftVersionInfo? version = Find(id);

            if (version == null)
                return false;

            Cache.Lookup.Remove(id);

            GetCollection(version.Type).Remove(version);

            return true;
        }

        // Rebuild lookup dictionary from current cache
        public static void RebuildLookup()
        {
            Cache.Lookup.Clear();

            AddToLookup(Cache.Releases);
            AddToLookup(Cache.Snapshots);
            AddToLookup(Cache.OldBetas);
            AddToLookup(Cache.OldAlphas);
        }

        // Add versions to lookup dictionary
        private static void AddToLookup(IEnumerable<MinecraftVersionInfo> versions)
        {
            foreach (MinecraftVersionInfo version in versions)
            {
                Cache.Lookup[version.Id] = version;
            }
        }

        // Replace current cache with new one and rebuild lookup
        public static void ReplaceCache(MinecraftVersionCache cache)
        {
            Cache = cache;
            RebuildLookup();
        }

        // Get collection of versions based on type
        private static List<MinecraftVersionInfo> GetCollection(MinecraftVersionType type)
        {
            return type switch
            {
                MinecraftVersionType.Release => Cache.Releases,
                MinecraftVersionType.Snapshot => Cache.Snapshots,
                MinecraftVersionType.OldBeta => Cache.OldBetas,
                MinecraftVersionType.OldAlpha => Cache.OldAlphas,

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        // Get all versions from cache
        public static IEnumerable<MinecraftVersionInfo> GetVersions()
        {
            return Cache.Releases.Concat(Cache.Snapshots)
                                 .Concat(Cache.OldBetas)
                                 .Concat(Cache.OldAlphas);
        }
    }
}