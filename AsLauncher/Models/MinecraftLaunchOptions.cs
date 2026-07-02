namespace AsLauncher.Models
{
    // Minecraft launch options used for <version>.json rules
    public static class MinecraftLaunchOptions
    {
        // Launch demo mode
        public static bool DemoMode { get; set; } = false;

        // Launch with custom resolution
        public static bool CustomResolution { get; set; } = false;

        // Enable Quick Play (future)
        public static bool QuickPlay { get; set; } = false;

        // Enable multiplayer (future)
        public static bool Multiplayer { get; set; } = true;

        // Enable Realms (future)
        public static bool Realms { get; set; } = true;
    }
}