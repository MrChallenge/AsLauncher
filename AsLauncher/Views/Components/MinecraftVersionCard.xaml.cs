using AsLauncher.Core;
using AsLauncher.Models;
using AsLauncher.Resources.Localization;
using AsLauncher.Services;
using System.ComponentModel;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;

using Localization = AsLauncher.Resources.Localization.Resources;

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

        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
            nameof(Version),
            typeof(MinecraftVersionEntry),
            typeof(MinecraftVersionCard),
            new PropertyMetadata(null, OnVersionChanged));

        public MinecraftVersionEntry Version
        {
            get => (MinecraftVersionEntry)GetValue(VersionProperty);
            set => SetValue(VersionProperty, value);
        }

        private void UpdateMinecraftVersionButtons()
        {
            if (Version == null)
                return;

            MinecraftVersionRemoveButton.Visibility = Visibility.Collapsed;

            switch (Version.InstallState)
            {
                // Install
                case MinecraftVersionInstallState.NotInstalled:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonInstall;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Green;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                // Cancel
                case MinecraftVersionInstallState.Downloading:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonCancel;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Blue;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                // Installing
                case MinecraftVersionInstallState.Installing:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonInstalling;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Blue;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                // Launch
                case MinecraftVersionInstallState.Installed:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonLaunch;
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

                // Removing
                case MinecraftVersionInstallState.Removing:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonRemoving;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Red;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                // Restore
                case MinecraftVersionInstallState.Removed:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonRestore;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.White;
                    MinecraftVersionMainButton.ButtonForeground = Theme.Middleground;

                    break;

                // Corrupted
                case MinecraftVersionInstallState.Corrupted:

                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonCorrupted;
                    MinecraftVersionMainButton.IsEnabled = false;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Yellow;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonForeground = Theme.White;

                    break;

                // Reinstall
                case MinecraftVersionInstallState.Reinstall:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonReinstall;
                    MinecraftVersionMainButton.IsEnabled = true;

                    MinecraftVersionMainButton.ButtonBorderBrush = Theme.Transparent;
                    MinecraftVersionMainButton.ButtonBackground = Theme.Yellow;
                    MinecraftVersionMainButton.ButtonForeground = Theme.Middleground;

                    break;

                // Unavailable
                case MinecraftVersionInstallState.Unavailable:
                    MinecraftVersionMainButton.ButtonContent = Localization.ButtonUnavailable;
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
                case "release": VersionTypeIndicator.Fill = Theme.Green;

                    break;

                case "snapshot": VersionTypeIndicator.Fill = Theme.Yellow;

                    break;

                case "old_beta": VersionTypeIndicator.Fill = Theme.Red;

                    break;

                case "old_alpha": VersionTypeIndicator.Fill = Theme.LightBlue;

                    break;

                default: VersionTypeIndicator.Fill = Theme.White;

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

        // Install manager
        private async void MinecraftVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (Version == null)
                return;

            switch (Version.InstallState)
            {
                // Installing
                case MinecraftVersionInstallState.NotInstalled:

                    try
                    {
                        Version.CancellationTokenSource = new();

                        Version.Progress = 0;
                        Version.IsProgressVisible = Visibility.Visible;

                        Version.InstallState = MinecraftVersionInstallState.Downloading;

                        await MinecraftVersionManager.InstallVersionAsync(Version, Version.CancellationTokenSource.Token);

                        Version.InstallState = MinecraftVersionInstallState.Installing;

                        await Task.Delay(Theme.InstallStateDelay);

                        Version.InstallState = MinecraftVersionInstallState.Installed;

                        Version.Progress = 100;
                        Version.IsProgressVisible = Visibility.Collapsed;
                    }
                    catch (OperationCanceledException)
                    {
                        Version.Progress = 0;
                        Version.IsProgressVisible = Visibility.Collapsed;

                        MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                        Version.InstallState = MinecraftVersionInstallState.NotInstalled;
                    }
                    catch (Exception ex)
                    {
                        Version.Progress = 0;
                        Version.IsProgressVisible = Visibility.Collapsed;

                        MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                        MessageBox.Show(ex.Message);

                        Version.InstallState = MinecraftVersionInstallState.NotInstalled;
                    }

                    break;

                // Downloading -> Canceling
                case MinecraftVersionInstallState.Downloading:

                    Version.CancellationTokenSource?.Cancel();

                    break;

                // Launching
                case MinecraftVersionInstallState.Installed:
                    {
                        using JsonDocument document = MinecraftVersionManager.LoadVersionJson(Version.Id);
                        
                        string mainClass = document.RootElement
                                                   .GetProperty("mainClass")
                                                   .GetString()!;

                        string classPath = MinecraftVersionManager.BuildClassPath(Version.Id);

                        MessageBox.Show($"MainClass:\n{mainClass}\n\nClasspath entries:{classPath.Split(System.IO.Path.PathSeparator).Length}");

                        List<string> jvmArguments = MinecraftVersionManager.GetJvmArguments(Version.Id);

                        List<string> gameArguments = MinecraftVersionManager.GetGameArguments(Version.Id);

                        MessageBox.Show($"JVM: {jvmArguments.Count}\nGame: {gameArguments.Count}");

                        break;
                    }

                // Removed -> Restoring
                case MinecraftVersionInstallState.Removed:

                    Version.InstallState = MinecraftVersionInstallState.Installing;

                    await Task.Delay(Theme.InstallStateDelay);

                    MinecraftVersionManager.RestoreVersion(Version.Id);

                    Version.InstallState = MinecraftVersionInstallState.Installed;

                    break;

                // Corrupted -> Reinstalling
                case MinecraftVersionInstallState.Reinstall:

                    MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                    Version.CancellationTokenSource = new();

                    Version.InstallState = MinecraftVersionInstallState.Downloading;

                    await MinecraftVersionManager.InstallVersionAsync(Version, Version.CancellationTokenSource.Token);

                    Version.InstallState = MinecraftVersionInstallState.Installing;

                    await Task.Delay(Theme.InstallStateDelay);

                    Version.InstallState = MinecraftVersionInstallState.Installed;

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