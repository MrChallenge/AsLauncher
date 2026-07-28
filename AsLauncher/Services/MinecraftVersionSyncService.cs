using AsLauncher.Models;
using System.Net.Http;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class MinecraftVersionSyncService
    {
        private const string ManifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";

        private static readonly HttpClient Http = new();

        public static async Task<MojangManifest?> DownloadManifestAsync()
        {
            try
            {
                string json = await Http.GetStringAsync(ManifestUrl);

                return JsonSerializer.Deserialize<MojangManifest>(json);
            }
            catch
            {
                return null;
            }
        }

        private static MinecraftVersionInfo Convert(MojangVersion version)
        {
            return new MinecraftVersionInfo
            {
                Id = version.Id,
                Type = MinecraftVersionTypeExtensions.FromManifest(version.Type),
                Url = version.Url,
                ReleaseTime = version.ReleaseTime
            };
        }

        private static void SyncCategory(IEnumerable<MojangVersion> versions, MinecraftVersionType type)
        {
            foreach (MojangVersion version in versions)
            {
                if (MinecraftVersionTypeExtensions.FromManifest(version.Type) != type)
                    continue;

                MinecraftVersionInfo info = Convert(version);

                if (MinecraftVersionCacheService.Contains(info.Id))
                    break;

                MinecraftVersionCacheService.AddVersion(info);
            }
        }

        public static async Task SyncAsync()
        {
            MojangManifest? manifest = await DownloadManifestAsync();

            if (manifest == null)
                return;

            SyncCategory(manifest.Versions, MinecraftVersionType.Release);
            SyncCategory(manifest.Versions, MinecraftVersionType.Snapshot);
            SyncCategory(manifest.Versions, MinecraftVersionType.OldBeta);
            SyncCategory(manifest.Versions, MinecraftVersionType.OldAlpha);

            MinecraftVersionCacheStorage.Save();
        }
    }
}