using AsLauncher.Models;

namespace AsLauncher.Models
{
    public class LauncherSettings
    {
        public bool ShowReleases { get; set; } = true;

        public bool ShowSnapshots { get; set; } = true;

        public bool ShowBetas { get; set; } = true;

        public bool ShowAlphas { get; set; } = true;

        public LauncherPage LastPage { get; set; } = LauncherPage.General;
    }
}