using AsLauncher.Core;
using AsLauncher.Models;
using AsLauncher.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace AsLauncher.Views.Pages
{
    public partial class VanillaPage : UserControl
    {
        public ObservableCollection<MinecraftVersionEntry> Versions { get; } = new();

        private List<MinecraftVersionEntry> AllVersions = new();

        // Initialize
        public VanillaPage()
        {
            InitializeComponent();

            DataContext = this;

            Loaded += VanillaPage_Loaded;
        }

        // Default filter settings
        public bool ShowReleases { get; set; } = true;

        public bool ShowSnapshots { get; set; } = true;

        public bool ShowBetas { get; set; } = true;

        public bool ShowAlphas { get; set; } = true;

        // Check if version matches search query
        private static bool MatchesSearch(string versionId, string search)
        {
            versionId = versionId.ToLower();
            search = search.ToLower();

            string normalizedVersion = versionId.Replace(".", "")
                                                .Replace("_", "")
                                                .Replace("-", "");

            string normalizedSearch = search.Replace(".", "")
                                            .Replace(" ", "")
                                            .Replace("_", "")
                                            .Replace("-", "");

            return
                versionId.Contains(search) ||
                normalizedVersion.Contains(normalizedSearch);
        }

        // Refresh versions based on filters and search query
        private void RefreshVersions()
        {
            Versions.Clear();

            string search = SearchBox?.Text?.ToLower() ?? "";

            foreach (var version in AllVersions)
            {
                if (version.Type == "release" && ReleaseCheckBox.IsChecked != true)
                    continue;

                if (version.Type == "snapshot" && SnapshotCheckBox.IsChecked != true)
                    continue;

                if (version.Type == "old_beta" && BetaCheckBox.IsChecked != true)
                    continue;

                if (version.Type == "old_alpha" && AlphaCheckBox.IsChecked != true)
                    continue;

                if (!string.IsNullOrWhiteSpace(search) && !MatchesSearch(version.Id, search))
                {
                    continue;
                }

                Versions.Add(version);
            }
        }

        // Refresh versions when search query changes
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshVersions();
        }

        // Refresh versions when filter checkboxes change
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            RefreshVersions();
        }

        // Load versions when page is loaded
        private async void VanillaPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var versions = await MinecraftVersionService.LoadVersions();

            foreach (var version in versions)
            {
                if (MinecraftVersionManager.IsVersionInstalled(version.Id))
                {
                    version.InstallState = MinecraftVersionInstallState.Installed;
                }
                else if (MinecraftVersionManager.IsVersionDeleted(version.Id))
                {
                    version.InstallState = MinecraftVersionInstallState.Removed;
                }
                else
                {
                    version.InstallState = MinecraftVersionInstallState.NotInstalled;
                }
            }

            AllVersions = versions.ToList();

            RefreshVersions();
        }
    }
}

