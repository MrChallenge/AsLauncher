using AsLauncher.Models;
using System.Net.Http;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class MinecraftVersionService
    {
        // Minecraft version manifest URL
        private const string ManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";

        // Load versions from manifest
        public static async Task<List<MinecraftVersionEntry>> LoadVersions()
        {
            using HttpClient client = new();

            string json = await client.GetStringAsync(ManifestUrl);

            using JsonDocument document = JsonDocument.Parse(json);

            List<MinecraftVersionEntry> versions = new();

            foreach (JsonElement version in document.RootElement.GetProperty("versions").EnumerateArray())
            {
                versions.Add(new MinecraftVersionEntry
                {
                    Id = version.GetProperty("id").GetString() ?? "",
                    Type = version.GetProperty("type").GetString() ?? "",
                    Url = version.GetProperty("url").GetString() ?? ""
                });
            }

            return versions;
        }
    }
}