namespace AsLauncher.Models
{
    public class MinecraftAccount
    {
        // Properties for MinecraftAccount class
        public string UserName { get; set; } = "";

        public string Uuid { get; set; } = "";

        public string AccessToken { get; set; } = "0";

        public string UserType { get; set; } = "legacy";

        public bool IsOffline { get; set; }
    }
}