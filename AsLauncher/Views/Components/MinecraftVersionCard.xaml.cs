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

        private void UpdateButtons()
        {
            if (Version == null)
                return;

            RemoveButton.Visibility = Visibility.Collapsed;

            switch (Version.InstallState)
            {
                // Install
                case MinecraftVersionInstallState.NotInstalled:
                    MainButton.ButtonContent = Localization.ButtonInstall;
                    MainButton.IsEnabled = true;

                    MainButton.ButtonBorderBrush = Theme.Transparent;
                    MainButton.ButtonBackground = Theme.Green;
                    MainButton.ButtonForeground = Theme.White;

                    break;

                // Cancel
                case MinecraftVersionInstallState.Downloading:
                    MainButton.ButtonContent = Localization.ButtonCancel;
                    MainButton.IsEnabled = true;

                    MainButton.ButtonBorderBrush = Theme.Transparent;
                    MainButton.ButtonBackground = Theme.Blue;
                    MainButton.ButtonForeground = Theme.White;

                    break;

                // Installing
                case MinecraftVersionInstallState.Installing:
                    MainButton.ButtonContent = Localization.ButtonInstalling;
                    MainButton.IsEnabled = false;

                    MainButton.ButtonBorderBrush = Theme.Blue;
                    MainButton.ButtonBackground = Theme.Transparent;
                    MainButton.ButtonForeground = Theme.White;

                    break;

                // Launch
                case MinecraftVersionInstallState.Installed:
                    MainButton.ButtonContent = Localization.ButtonLaunch;
                    MainButton.IsEnabled = true;

                    MainButton.ButtonBorderBrush = Theme.Transparent;
                    MainButton.ButtonBackground = Theme.Green;
                    MainButton.ButtonForeground = Theme.White;

                    // Remove
                    RemoveButton.Visibility = Visibility.Visible;

                    MainButton.ButtonBorderBrush = Theme.Transparent;
                    RemoveButton.ButtonBackground = Theme.Red;
                    RemoveButton.ButtonForeground = Theme.White;

                    break;

                // Removing
                case MinecraftVersionInstallState.Removing:
                    MainButton.ButtonContent = Localization.ButtonRemoving;
                    MainButton.IsEnabled = false;

                    MainButton.ButtonBorderBrush = Theme.Red;
                    MainButton.ButtonBackground = Theme.Transparent;
                    MainButton.ButtonForeground = Theme.White;

                    break;

                // Restore
                case MinecraftVersionInstallState.Removed:
                    MainButton.ButtonContent = Localization.ButtonRestore;
                    MainButton.IsEnabled = true;

                    MainButton.ButtonBorderBrush = Theme.Transparent;
                    MainButton.ButtonBackground = Theme.White;
                    MainButton.ButtonForeground = Theme.Middleground;

                    break;

                // Corrupted
                case MinecraftVersionInstallState.Corrupted:

                    MainButton.ButtonContent = Localization.ButtonCorrupted;
                    MainButton.IsEnabled = false;

                    MainButton.ButtonBorderBrush = Theme.Yellow;
                    MainButton.ButtonBackground = Theme.Transparent;
                    MainButton.ButtonForeground = Theme.White;

                    break;

                // Reinstall
                case MinecraftVersionInstallState.Reinstall:
                    MainButton.ButtonContent = Localization.ButtonReinstall;
                    MainButton.IsEnabled = true;

                    MainButton.ButtonBorderBrush = Theme.Transparent;
                    MainButton.ButtonBackground = Theme.Yellow;
                    MainButton.ButtonForeground = Theme.Middleground;

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

            card.UpdateButtons();
        }

        // Update buttons when version's install state changes
        private void Version_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MinecraftVersionEntry.InstallState))
            {
                Dispatcher.Invoke(UpdateButtons);
            }
        }

        // Install manager
        private async void MainButton_Click(object sender, RoutedEventArgs e)
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

                        Version.InstallState = MinecraftVersionInstallState.Downloading;

                        await MinecraftVersionManager.InstallVersionAsync(Version, Version.CancellationTokenSource.Token);

                        Version.InstallState = MinecraftVersionInstallState.Installing;

                        await Task.Delay(300);

                        Version.InstallState = MinecraftVersionInstallState.Installed;
                    }
                    catch (OperationCanceledException)
                    {
                        MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                        Version.InstallState = MinecraftVersionInstallState.NotInstalled;
                    }
                    catch (Exception ex)
                    {
                        MinecraftVersionManager.CleanupIncompleteVersion(Version.Id);

                        MessageBox.Show(ex.Message);

                        Version.InstallState = MinecraftVersionInstallState.NotInstalled;
                    }

                    break;

                // Canceling
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

                // Restoring
                case MinecraftVersionInstallState.Removed:

                    Version.InstallState = MinecraftVersionInstallState.Installing;

                    await Task.Delay(300);

                    MinecraftVersionManager.RestoreVersion(Version.Id);

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

            await Task.Delay(300);

            MinecraftVersionManager.DeleteVersion(Version.Id);

            Version.InstallState = MinecraftVersionInstallState.Removed;
        }

        // Update buttons and version indicator when card is loaded
        private void MinecraftVersionCard_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateButtons();

            UpdateVersionIndicator();
        }
    }
}