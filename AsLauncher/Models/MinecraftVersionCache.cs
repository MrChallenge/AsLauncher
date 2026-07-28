using System.Text.Json.Serialization;

namespace AsLauncher.Models
{
    public class MinecraftVersionCache
    {
        public int CacheVersion { get; set; } = 1;

        public DateTime LastSync { get; set; }

        public List<MinecraftVersionInfo> Releases { get; set; } = [];

        public List<MinecraftVersionInfo> Snapshots { get; set; } = [];

        public List<MinecraftVersionInfo> OldBetas { get; set; } = [];

        public List<MinecraftVersionInfo> OldAlphas { get; set; } = [];

        [JsonIgnore]

        public Dictionary<string, MinecraftVersionInfo> Lookup { get; set; } = [];
    }
}
