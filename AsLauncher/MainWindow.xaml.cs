using AsLauncher.Core;
using AsLauncher.Models;
using AsLauncher.Services;
using AsLauncher.Views.Pages;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AsLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(Core.Theme.LauncherIcon);

            Closing += MainWindow_Closing;

            UpdateSidebarState();

            SettingsManager.Load();

            JavaRuntimeManager.Initialize();

            MinecraftVersionManager.Initialize();

            switch (SettingsManager.Settings.LastPage)
            {
                case LauncherPage.Vanilla: MainContent.Content = new VanillaPage();

                    break;

                case LauncherPage.Modpacks: MainContent.Content = new ModpacksPage();

                    break;

                case LauncherPage.Configs: MainContent.Content = new ConfigsPage();

                    break;

                default: MainContent.Content = new GeneralPage();

                    break;
            }
        }

        private bool _sidebarCollapsed = false;

        private void UpdateSidebarState()
        {
            if (_sidebarCollapsed)
            {
                SidebarColumn.Width = new GridLength(72);

                SidebarTitle.Text = "As";

                NavGeneralText.Visibility = Visibility.Collapsed;
                NavVanillaText.Visibility = Visibility.Collapsed;
                NavModpacksText.Visibility = Visibility.Collapsed;
                NavConfigsText.Visibility = Visibility.Collapsed;

                SidebarToggleIcon.RenderTransform = new RotateTransform(0);
            }
            else
            {
                SidebarColumn.Width = new GridLength(240);

                SidebarTitle.Text = "AsLauncher";

                NavGeneralText.Visibility = Visibility.Visible;
                NavVanillaText.Visibility = Visibility.Visible;
                NavModpacksText.Visibility = Visibility.Visible;
                NavConfigsText.Visibility = Visibility.Visible;

                SidebarToggleIcon.RenderTransform = new RotateTransform(180);
            }
        }

        private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;

            UpdateSidebarState();
        }

        private void GeneralButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.General;

            SettingsManager.Save();

            MainContent.Content = new GeneralPage();
        }

        private void VanillaButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.Vanilla;

            SettingsManager.Save();

            MainContent.Content = new VanillaPage();
        }

        private void ModpacksButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.Modpacks;

            SettingsManager.Save();

            MainContent.Content = new ModpacksPage();
        }

        private void ConfigsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.Configs;

            SettingsManager.Save();

            MainContent.Content = new ConfigsPage();
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            JavaRuntimeManager.CleanupDeletedFolder();

            MinecraftVersionManager.CleanupDeletedFolder();
        }
    }
}