using AsLauncher.Models;
using System.IO;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher-settings.json");

        public static LauncherSettings Settings { get; private set; } = new();

        public static void Load()
        {
            if (!File.Exists(SettingsFile))
            {
                Save();

                return;
            }

            string json = File.ReadAllText(SettingsFile);

            Settings = JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();
        }

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