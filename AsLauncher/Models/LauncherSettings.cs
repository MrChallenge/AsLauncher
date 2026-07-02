namespace AsLauncher.Models
{
    public class LauncherSettings
    {
        // Default settings for Launcher
        public bool ShowReleases { get; set; } = true;

        public bool ShowSnapshots { get; set; } = false;

        public bool ShowBetas { get; set; } = false;

        public bool ShowAlphas { get; set; } = false;

        public LauncherPage LastPage { get; set; } = LauncherPage.General;

        public string GeneratedPlayerName { get; set; } = "";

        public string PlayerName { get; set; } = "";
    }
}