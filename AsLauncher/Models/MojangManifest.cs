using System.Text.Json.Serialization;

namespace AsLauncher.Models
{
    public class MojangManifest
    {
        [JsonPropertyName("versions")]
        public List<MojangVersion> Versions { get; set; } = [];
    }

    public class MojangVersion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("releaseTime")]
        public DateTime ReleaseTime { get; set; }
    }
}