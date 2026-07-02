using AsLauncher.Models;

namespace AsLauncher.Services
{
    public static class MinecraftAccountManager
    {
        // Create MinecraftAccount instance based on current LauncherSettings
        public static MinecraftAccount GetCurrentAccount()
        {
            return CreateOfflineAccount(SettingsManager.Settings.PlayerName);
        }

        // Create offline MinecraftAccount using specified playerName
        public static MinecraftAccount CreateOfflineAccount(string playerName)
        {
            return new MinecraftAccount
            {
                UserName = playerName,
                Uuid = GenerateOfflineUuid(playerName),
                AccessToken = "0",
                UserType = "legacy",
                IsOffline = true
            };
        }

        // Generate UUID for playerName using MD5 hashing
        private static string GenerateOfflineUuid(string playerName)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes($"OfflinePlayer:{playerName}");

            byte[] hash = md5.ComputeHash(bytes);

            return new Guid(hash).ToString();
        }
    }
}