using AsLauncher.Core;
using AsLauncher.Core.Logger;
using AsLauncher.Models;
using AsLauncher.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Lang = AsLauncher.Resources.Localization.Resources;

namespace AsLauncher.Views.Components
{
    public partial class MinecraftVersionCard : UserControl
    {
        // Initialize
        public MinecraftVersionCard()
        {
            InitializeComponent();

            Loaded += MinecraftVersionCard_Loaded;

        }

        // Dependency Properties
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
            nameof(Version),
            typeof(MinecraftVersionEntry),
            typeof(MinecraftVersionCard),
            new PropertyMetadata(null, OnVersionChanged));

        // Version property
        public MinecraftVersionEntry Version
        {
            get => (MinecraftVersionEntry)GetValue(VersionProperty);
            set => SetValue(VersionProperty, value);
        }

        // Update buttons based on version's install state
        private void UpdateMinecraftVersionButtons()
        {
            if (Version == null)
                return;

            MinecraftVersionRemoveButton.Visibility = Visibility.Collapsed;

            switch (Version.InstallState)
            {
               case MinecraftVersionInstallState.NotInstalled:   // Install
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonInstall;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Green;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

               case MinecraftVersionInstallState.Downloading:   // Cancel
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonCancel;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Blue;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

               case MinecraftVersionInstallState.Installing:   // Installing
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonInstalling;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Blue;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                case MinecraftVersionInstallState.Installed:   // Launch
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonLaunch;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Green;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    // Remove
                    MinecraftVersionRemoveButton.Visibility = Visibility.Visible;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionRemoveButton.ButtonBackground = Theme.Red;
                    MinecraftVersionRemoveButton.ButtonForeground = Theme.White;

                    break;

               case MinecraftVersionInstallState.Removing:   // Removing
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonRemoving;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Red;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

               case MinecraftVersionInstallState.Removed:   // Restore
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonRestore;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.White;
                    MinecraftVersionMainButton.ButtonForeground = Theme.Middleground;

                    break;

                case MinecraftVersionInstallState.Corrupted:   // Corrupted

                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonCorrupted;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Yellow;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                case MinecraftVersionInstallState.Reinstall:   // Reinstall
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonReinstall;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Yellow;
                    MinecraftVersionMainButton.ButtonForeground = Theme.Middleground;

                    break;

                case MinecraftVersionInstallState.Unavailable:   // Unavailable
                    MinecraftVersionMainButton.ButtonContent = Lang.ButtonUnavailable;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Grey;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;
            }
        }

        // Version type indicator color based on version type
        private void UpdateVersionIndicator()
        {
            if (Version == null)
                return;

            switch (Version.Type)
            {
                case MinecraftVersionType.Release:VersionTypeIndicator.Fill = Theme.Green;
                    break;

                case MinecraftVersionType.Snapshot:VersionTypeIndicator.Fill = Theme.Yellow;
                    break;

                case MinecraftVersionType.OldBeta:VersionTypeIndicator.Fill = Theme.Red;
                    break;

                case MinecraftVersionType.OldAlpha:VersionTypeIndicator.Fill = Theme.LightBlue;
                    break;

                default:VersionTypeIndicator.Fill = Theme.White;
                    break;
            }
        }

        // Update event handlers when version changes
        private static void OnVersionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MinecraftVersionCard card)
                return;

            if (e.OldValue is MinecraftVersionEntry oldVersion)
            {
                oldVersion.PropertyChanged -= card.Version_PropertyChanged;
            }

            if (e.NewValue is MinecraftVersionEntry newVersion)
            {
                newVersion.PropertyChanged += card.Version_PropertyChanged;
            }

            card.UpdateMinecraftVersionButtons();
        }

        // Update buttons when version's install state changes
        private void Version_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MinecraftVersionEntry.InstallState))
            {
                Dispatcher.Invoke(UpdateMinecraftVersionButtons);
            }
        }

        // Install process
        private async Task InstallProcess(bool cleanupBeforeInstall)
        {
            if (Version == null)
                return;

            try
            {
                Version.CancellationTokenSource = new();

                Version.DownloadProgress = 0;
                Version.SetupProgress = 0;
                Version.SetupStatus = "";

                Version.ProgressBarBrush = Theme.LightBlue;
                Version.IsDownloadProgressVisible = Visibility.Visible;

                if (cleanupBeforeInstall)
                {
                    MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);
                }

                Version.InstallState = MinecraftVersionInstallState.Downloading;

                await MinecraftVersionManager.InstallVersionAsync(Version, Version.CancellationTokenSource.Token);

                Logger.Info(LoggerConfig.VersionsSource, $"Version {Version.Id} downloaded.");

                Version.InstallState = MinecraftVersionInstallState.Installing;

                Logger.Info(LoggerConfig.VersionsSource, $"Validating {Version.Id}...");

                Version.IsDownloadProgressVisible = Visibility.Collapsed;

                Version.ProgressBarBrush = Theme.Green;
                Version.IsSetupProgressVisible = Visibility.Visible;

                bool valid = await MinecraftVersionManager.ValidateVersionAsync(Version.Id,
                    progress => Dispatcher.Invoke(() => Version.SetupProgress = progress),
                    status => Dispatcher.Invoke(() => Version.SetupStatus = status));

                Logger.Info(LoggerConfig.VersionsSource, valid
                    ? $"Version {Version.Id} validated successfully."
                    : $"Version {Version.Id} validation failed.");

                Version.IsSetupProgressVisible = Visibility.Collapsed;
                Version.SetupProgress = 0;
                Version.SetupStatus = "";

                if (!valid)
                {
                    MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                    Logger.Warning(LoggerConfig.VersionsSource, $"Incomplete installation of {Version.Id} was removed.");
                }

                await Task.Delay(Theme.InstallStateDelay);

                Version.InstallState = valid
                    ? MinecraftVersionInstallState.Installed
                    : MinecraftVersionInstallState.NotInstalled;

                Logger.Success(LoggerConfig.VersionsSource, $"{Version.Id} installed successfully.");

                Version.IsDownloadProgressVisible = Visibility.Collapsed;
            }
            catch (OperationCanceledException)   // if canceled
            {
                Version.DownloadProgress = 0;
                Version.SetupProgress = 0;
                Version.SetupStatus = "";

                Version.IsDownloadProgressVisible = Visibility.Collapsed;

                Version.IsSetupProgressVisible = Visibility.Collapsed;

                MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                Logger.Info(LoggerConfig.VersionsSource, $"Installation of {Version.Id} cancelled.");

                Version.InstallState = MinecraftVersionInstallState.NotInstalled;
            }
            catch (Exception ex)   // if eror happens
            {
                Version.DownloadProgress = 0;
                Version.SetupProgress = 0;
                Version.SetupStatus = "";

                Version.IsDownloadProgressVisible = Visibility.Collapsed;

                Version.IsSetupProgressVisible = Visibility.Collapsed;

                MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                Logger.Error(LoggerConfig.VersionsSource, ex.ToString());

                Version.InstallState = MinecraftVersionInstallState.NotInstalled;
            }
        }

        // Install manager
        private async void MinecraftVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (Version == null)
                return;

            switch (Version.InstallState)
            {
                case MinecraftVersionInstallState.NotInstalled:   // Installing

                    await InstallProcess(false);

                    break;

                case MinecraftVersionInstallState.Downloading:   // Downloading -> Canceling

                    Version.CancellationTokenSource?.Cancel();

                    break;

                case MinecraftVersionInstallState.Installed:   // Launching
                    {
                        await MinecraftLaunchManager.LaunchAsync(Version.Id);

                        break;
                    }

                case MinecraftVersionInstallState.Removed:   // Removed -> Restoring

                    Version.InstallState = MinecraftVersionInstallState.Installing;

                    await Task.Delay(Theme.InstallStateDelay);

                    MinecraftVersionManager.RestoreVersion(Version.Id);

                    bool restoreValid = await MinecraftVersionManager.ValidateVersionAsync(Version.Id, null, null);

                    Version.InstallState = restoreValid
                        ? MinecraftVersionInstallState.Installed
                        : MinecraftVersionInstallState.NotInstalled;

                    break;

                case MinecraftVersionInstallState.Reinstall:   // Corrupted -> Reinstalling

                    await InstallProcess(true);

                    break;
            }
        }

        // Remove manager
        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Version == null)
                return;

            Version.InstallState = MinecraftVersionInstallState.Removing;

            await Task.Delay(Theme.InstallStateDelay);

            MinecraftVersionManager.DeleteVersion(Version.Id);

            Version.InstallState = MinecraftVersionInstallState.Removed;
        }

        // Update buttons and version indicator when card is loaded
        private void MinecraftVersionCard_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMinecraftVersionButtons();

            UpdateVersionIndicator();
        }
    }
}