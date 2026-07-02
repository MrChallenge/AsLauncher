using AsLauncher.Core;
using AsLauncher.Models;
using System.IO;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class SettingsManager
    {
        // Path to settings file in app directory
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher-settings.json");

        // Launcher settings instance
        public static LauncherSettings Settings { get; private set; } = new();

        // Load settings from file or create new settings if file doesn't exist
        public static void Load()
        {
            if (!File.Exists(SettingsFile))
            {
                Save();

                return;
            }

            string json = File.ReadAllText(SettingsFile);

            Settings = JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();

            if (string.IsNullOrWhiteSpace(Settings.GeneratedPlayerName))
            {
                Settings.GeneratedPlayerName = Theme.GenerateDefaultPlayerName();
            }

            if (string.IsNullOrWhiteSpace(Settings.PlayerName))
            {
                Settings.PlayerName = Settings.GeneratedPlayerName;
            }

            Save();
        }

        // Save settings to file
        public static void Save()
        {
            string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFile, json);
        }
    }
}