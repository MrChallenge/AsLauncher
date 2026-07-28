namespace AsLauncher.Models
{
    public static class MinecraftVersionTypeExtensions
    {
        public static MinecraftVersionType FromManifest(string type)
        {
            return type switch
            {
                "release" => MinecraftVersionType.Release,
                "snapshot" => MinecraftVersionType.Snapshot,
                "old_beta" => MinecraftVersionType.OldBeta,
                "old_alpha" => MinecraftVersionType.OldAlpha,

                _ => throw new ArgumentException($"Unknown version type: {type}")
            };
        }

        public static string ToManifest(this MinecraftVersionType type)
        {
            return type switch
            {
                MinecraftVersionType.Release => "release",
                MinecraftVersionType.Snapshot => "snapshot",
                MinecraftVersionType.OldBeta => "old_beta",
                MinecraftVersionType.OldAlpha => "old_alpha",

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}