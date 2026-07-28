using AsLauncher.Models;

namespace AsLauncher.Services
{
    public static class MinecraftVersionService
    {
        public static async Task<List<MinecraftVersionEntry>> LoadVersions()
        {
            await MinecraftVersionManager.InitializeAsync();

            return MinecraftVersionCacheService
                .GetVersions()
                .Select(version => new MinecraftVersionEntry
                {
                    Id = version.Id,
                    Type = version.Type,
                    Url = version.Url
                })
                .ToList();
        }
    }
}