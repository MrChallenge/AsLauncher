using AsLauncher.Models;
using AsLauncher.Services;
using System.Windows;
using System.Windows.Controls;
using AsLauncher.Core;

using Lang = AsLauncher.Resources.Localization.Resources;

namespace AsLauncher.Views.Components
{
    public partial class JavaRuntimeCard : UserControl
    {
        // Initialize
        public JavaRuntimeCard()
        {
            InitializeComponent();
        }

        // Dependency Properties
        public static readonly DependencyProperty MinecraftVersionProperty = DependencyProperty.Register(
            nameof(MinecraftVersion),
            typeof(string),
            typeof(JavaRuntimeCard));

        public static readonly DependencyProperty RuntimeListProperty = DependencyProperty.Register(
            nameof(RuntimeList),
            typeof(List<JavaRuntimeEntry>),
            typeof(JavaRuntimeCard));

        public List<JavaRuntimeEntry> RuntimeList
        {
            get => (List<JavaRuntimeEntry>)GetValue(RuntimeListProperty);
            set => SetValue(RuntimeListProperty, value);
        }

        public string MinecraftVersion
        {
            get => (string)GetValue(MinecraftVersionProperty);
            set => SetValue(MinecraftVersionProperty, value);
        }

        // Load ActionButton State
        private void ActionButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ActionButton button)
                return;

            if (button.Tag is not JavaRuntimeEntry runtime)
                return;

            JavaRuntimeButton_Click(button, runtime);

            runtime.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(JavaRuntimeEntry.InstallState))
                {
                    Dispatcher.Invoke(() =>
                    {
                        JavaRuntimeButton_Click(button, runtime);
                    });
                }
            };
        }

        private static void JavaRuntimeButton_Click(ActionButton JavaRuntimeMainButton, JavaRuntimeEntry JavaRuntime)
        {
            switch (JavaRuntime.InstallState)
            {
                // Install
                case JavaRuntimeInstallState.NotInstalled:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonInstall;
                    JavaRuntimeMainButton.IsEnabled = true;

                    JavaRuntimeMainButton.ButtonBackground = Theme.Green;
                    JavaRuntimeMainButton.ButtonForeground = Theme.White;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Transparent;

                    break;

                // Cancel
                case JavaRuntimeInstallState.Downloading:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonCancel;
                    JavaRuntimeMainButton.IsEnabled = true;

                    JavaRuntimeMainButton.ButtonBackground = Theme.Blue;
                    JavaRuntimeMainButton.ButtonForeground = Theme.White;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Transparent;

                    break;

                // Installing
                case JavaRuntimeInstallState.Installing:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonInstalling;
                    JavaRuntimeMainButton.IsEnabled = false;

                    JavaRuntimeMainButton.ButtonBackground = Theme.Transparent;
                    JavaRuntimeMainButton.ButtonForeground = Theme.White;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Blue;

                    break;

                // Remove
                case JavaRuntimeInstallState.Installed:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonRemove;
                    JavaRuntimeMainButton.IsEnabled = true;

                    JavaRuntimeMainButton.ButtonBackground = Theme.Red;
                    JavaRuntimeMainButton.ButtonForeground = Theme.White;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Transparent;

                    break;

                // Removing
                case JavaRuntimeInstallState.Removing:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonRemoving;
                    JavaRuntimeMainButton.IsEnabled = false;

                    JavaRuntimeMainButton.ButtonBackground = Theme.Transparent;
                    JavaRuntimeMainButton.ButtonForeground = Theme.White;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Red;

                    break;

                // Restore
                case JavaRuntimeInstallState.Removed:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonRestore;
                    JavaRuntimeMainButton.IsEnabled = true;

                    JavaRuntimeMainButton.ButtonBackground = Theme.White;
                    JavaRuntimeMainButton.ButtonForeground = Theme.Middleground;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Transparent;

                    break;

                case JavaRuntimeInstallState.Unavailable:
                    JavaRuntimeMainButton.ButtonContent = Lang.ButtonUnavailable;

                    JavaRuntimeMainButton.IsEnabled = false;

                    JavaRuntimeMainButton.ButtonBackground = Theme.Transparent;
                    JavaRuntimeMainButton.ButtonForeground = Theme.White;
                    JavaRuntimeMainButton.ButtonBorderBrush = Theme.Grey;

                    break;
            }
        }

        // Install/Remove ActionButton Click Handler
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ActionButton button)
                return;

            if (button.Tag is not JavaRuntimeEntry runtime)
                return;

            switch (runtime.InstallState)
            {
                case JavaRuntimeInstallState.NotInstalled:

                    runtime.CancellationTokenSource = new();

                    try
                    {
                        runtime.InstallState = JavaRuntimeInstallState.Downloading;   // Downloading

                        runtime.IsProgressVisible = Visibility.Visible;

                        string? archivePath = await JavaRuntimeManager.DownloadRuntime(
                            runtime.DownloadUrl,
                            runtime.RuntimeFolder + ".zip",
                            runtime.CancellationTokenSource.Token,
                            progress =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    runtime.Progress = progress;
                                });
                            });

                        if (archivePath == null)
                        {
                            runtime.InstallState = JavaRuntimeInstallState.NotInstalled;
                            return;
                        }

                        runtime.InstallState = JavaRuntimeInstallState.Installing;

                        await Task.Delay(Theme.InstallStateDelay);

                        bool installed = await JavaRuntimeManager.InstallRuntime(archivePath, runtime.RuntimeFolder);

                        runtime.Progress = 100;

                        runtime.IsProgressVisible = Visibility.Collapsed;

                        runtime.InstallState = installed
                            ? JavaRuntimeInstallState.Installed
                            : JavaRuntimeInstallState.NotInstalled;
                    }
                    catch (OperationCanceledException)   // if canceled: delete downloaded file
                    {
                        runtime.IsProgressVisible = Visibility.Collapsed;

                        runtime.Progress = 0;

                        JavaRuntimeManager.CleanupTemp(runtime.RuntimeFolder);

                        runtime.InstallState = JavaRuntimeInstallState.NotInstalled;
                    }
                    catch (Exception ex)   // if eror happens: delete downloaded file
                    {
                        runtime.IsProgressVisible = Visibility.Collapsed;

                        runtime.Progress = 0;

                        JavaRuntimeManager.CleanupTemp(runtime.RuntimeFolder);

                        runtime.InstallState = JavaRuntimeInstallState.NotInstalled;
                    }

                    break;

                case JavaRuntimeInstallState.Downloading:   // Cancel

                    runtime.CancellationTokenSource?.Cancel();

                    break;

                case JavaRuntimeInstallState.Installed:   // Remove

                    runtime.InstallState = JavaRuntimeInstallState.Removing;

                    await Task.Delay(Theme.InstallStateDelay);

                    bool deleted = JavaRuntimeManager.DeleteRuntime(runtime.RuntimeFolder);

                    runtime.InstallState = deleted
                        ? JavaRuntimeInstallState.Removed
                        : JavaRuntimeInstallState.Installed;

                    break;

                case JavaRuntimeInstallState.Removed:   // Restore

                    runtime.InstallState = JavaRuntimeInstallState.Installing;

                    await Task.Delay(Theme.InstallStateDelay);

                    bool restored = JavaRuntimeManager.RestoreRuntime(runtime.RuntimeFolder);

                    runtime.InstallState = restored
                        ? JavaRuntimeInstallState.Installed
                        :JavaRuntimeInstallState.Removed;

                    break;
            }
        }
    }
}