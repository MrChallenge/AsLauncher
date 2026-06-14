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

            ReleaseCheckBox.IsChecked = SettingsManager.Settings.ShowReleases;

            SnapshotCheckBox.IsChecked = SettingsManager.Settings.ShowSnapshots;

            BetaCheckBox.IsChecked = SettingsManager.Settings.ShowBetas;

            AlphaCheckBox.IsChecked = SettingsManager.Settings.ShowAlphas;

            Loaded += VanillaPage_Loaded;
        }

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
            if (!_settingsLoaded)
                return;

            SettingsManager.Settings.ShowReleases = ReleaseCheckBox.IsChecked == true;

            SettingsManager.Settings.ShowSnapshots = SnapshotCheckBox.IsChecked == true;

            SettingsManager.Settings.ShowBetas = BetaCheckBox.IsChecked == true;

            SettingsManager.Settings.ShowAlphas = AlphaCheckBox.IsChecked == true;

            SettingsManager.Save();

            RefreshVersions();
        }

        private bool _settingsLoaded;

        // Load versions when page is loaded
        private async void VanillaPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var versions = await MinecraftVersionService.LoadVersions();

            foreach (var version in versions)
            {
                if (MinecraftVersionManager.IsVersionCorrupted(version.Id))
                {
                    version.InstallState = MinecraftVersionInstallState.Corrupted;
                }
                else if (MinecraftVersionManager.IsVersionInstalled(version.Id))
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

            _settingsLoaded = true;

            RefreshVersions();
        }
    }
}

