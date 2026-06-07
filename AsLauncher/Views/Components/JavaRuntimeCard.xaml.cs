using AsLauncher.Models;
using AsLauncher.Services;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace AsLauncher.Views.Components
{
    public partial class JavaRuntimeCard : UserControl
    {
        public static readonly DependencyProperty MinecraftVersionProperty =
            DependencyProperty.Register(
                nameof(MinecraftVersion),
                typeof(string),
                typeof(JavaRuntimeCard));

        public static readonly DependencyProperty RuntimeListProperty =
            DependencyProperty.Register(
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

        public JavaRuntimeCard()
        {
            InitializeComponent();

            Loaded += JavaRuntimeCard_Loaded;
        }

        private void JavaRuntimeCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (RuntimeList == null)
                return;

            foreach (JavaRuntimeEntry runtime in RuntimeList)
            {
                bool installed = RuntimeManager.IsRuntimeInstalled(runtime.RuntimeFolder);

                bool deleted = RuntimeManager.IsRuntimeDeleted(runtime.RuntimeFolder);

                if (installed)
                {
                    runtime.InstallState = RuntimeInstallState.Installed;
                }
                else if (deleted)
                {
                    runtime.InstallState = RuntimeInstallState.Deleted;
                }
                else
                {
                    runtime.InstallState = RuntimeInstallState.NotInstalled;
                }
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not JavaRuntimeEntry runtime)
                return;

            switch (runtime.InstallState)
            {
                case RuntimeInstallState.NotInstalled:

                    runtime.CancellationTokenSource = new();

                    try
                    {
                        runtime.InstallState = RuntimeInstallState.Downloading;

                        runtime.IsProgressVisible = Visibility.Visible;

                        string? archivePath = await RuntimeManager.DownloadRuntime(
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
                            runtime.InstallState = RuntimeInstallState.NotInstalled;
                            return;
                        }

                        runtime.InstallState = RuntimeInstallState.Installing;

                        await Task.Delay(300);

                        bool installed = await RuntimeManager.InstallRuntime(archivePath, runtime.RuntimeFolder);

                        runtime.Progress = 100;

                        runtime.IsProgressVisible = Visibility.Collapsed;

                        runtime.InstallState = installed
                            ? RuntimeInstallState.Installed
                            : RuntimeInstallState.NotInstalled;
                    }
                    catch (OperationCanceledException)
                    {
                        runtime.IsProgressVisible = Visibility.Collapsed;

                        runtime.Progress = 0;

                        RuntimeManager.CleanupTemp(runtime.RuntimeFolder);

                        runtime.InstallState = RuntimeInstallState.NotInstalled;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());

                        runtime.IsProgressVisible = Visibility.Collapsed;

                        runtime.Progress = 0;

                        RuntimeManager.CleanupTemp(runtime.RuntimeFolder);

                        runtime.InstallState = RuntimeInstallState.NotInstalled;
                    }

                    break;

                case RuntimeInstallState.Downloading:

                    runtime.CancellationTokenSource?.Cancel();

                    break;

                case RuntimeInstallState.Installed:

                    runtime.InstallState = RuntimeInstallState.Removing;

                    await Task.Delay(300);

                    bool deleted = RuntimeManager.DeleteRuntime(runtime.RuntimeFolder);

                    runtime.InstallState = deleted
                        ? RuntimeInstallState.Deleted
                        : RuntimeInstallState.Installed;

                    break;

                case RuntimeInstallState.Deleted:

                    runtime.InstallState = RuntimeInstallState.Installing;

                    await Task.Delay(300);

                    bool restored = RuntimeManager.RestoreRuntime(runtime.RuntimeFolder);

                    runtime.InstallState = restored
                        ? RuntimeInstallState.Installed
                        :RuntimeInstallState.Deleted;

                    break;
            }
        }
    }
}