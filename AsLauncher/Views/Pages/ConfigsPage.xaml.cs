using AsLauncher.Models;
using AsLauncher.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AsLauncher.Views.Pages
{
    public partial class ConfigsPage : UserControl
    {
        // Initialize
        public ConfigsPage()
        {
            InitializeComponent();

            DataContext = this;

            Loaded += ConfigsPage_Loaded;

            _internetTimer.Interval = AsLauncher.Core.Theme.InternetCheckInterval;

            _internetTimer.Tick += InternetTimer_Tick;

            _internetTimer.Start();
        }

        // Java Runtime Groups
        public List<JavaRuntimeGroup> JavaRuntimeGroups { get; set; } = new();

        // Load Java Runtime Groups
        private async void ConfigsPage_Loaded(object sender, RoutedEventArgs e)
        {
            JavaRuntimeGroups = JavaRuntimeRegistryService.Load().Groups;

            DataContext = null;

            DataContext = this;

            bool internetAvailable = await MinecraftVersionManager.HasInternetAsync();

            foreach (var group in JavaRuntimeGroups)
            {
                foreach (var runtime in group.JavaRuntimes)
                {
                    runtime.InstallState = JavaRuntimeManager.GetRuntimeState(runtime.RuntimeFolder);

                    if (!internetAvailable && runtime.InstallState == JavaRuntimeInstallState.NotInstalled)
                    {
                        runtime.InstallState = JavaRuntimeInstallState.Unavailable;
                    }
                }
            }
        }

        // Internet Connectivity Timer
        private readonly DispatcherTimer _internetTimer = new();

        // Check Internet Connectivity
        private async void InternetTimer_Tick(object? sender, EventArgs e)
        {
            bool internetAvailable = await MinecraftVersionManager.HasInternetAsync();

            foreach (var group in JavaRuntimeGroups)
            {
                foreach (var runtime in group.JavaRuntimes)
                {
                    if (!internetAvailable)
                    {
                        if (runtime.InstallState == JavaRuntimeInstallState.NotInstalled)
                        {
                            runtime.InstallState = JavaRuntimeInstallState.Unavailable;
                        }
                    }
                    else
                    {
                        if (runtime.InstallState == JavaRuntimeInstallState.Unavailable)
                        {
                            runtime.InstallState = JavaRuntimeInstallState.NotInstalled;
                        }
                    }
                }
            }
        }

    }
}