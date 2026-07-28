namespace AsLauncher.Models
{
    public class MinecraftVersionInfo
    {
        public string Id { get; set; } = "";

        public string Url { get; set; } = "";

        public MinecraftVersionType Type { get; set; }

        public DateTime ReleaseTime { get; set; }
    }
}