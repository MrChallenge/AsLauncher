using AsLauncher.Core;
using AsLauncher.Models;
using AsLauncher.Services;
using AsLauncher.Views.Pages;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AsLauncher
{
    public partial class MainWindow : Window
    {
        // Initialize
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

            PlayerNameTextBox.Text = SettingsManager.Settings.PlayerName;

            PreviewMouseDown += (_, _) => Keyboard.ClearFocus();
        }

        // Sidebar state
        private bool _sidebarCollapsed = false;

        // Update sidebar state
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

        // Sidebar toggle button click event
        private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;

            UpdateSidebarState();
        }

        // General button click event
        private void GeneralButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.General;

            SettingsManager.Save();

            MainContent.Content = new GeneralPage();
        }

        // Vanila button click event
        private void VanillaButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.Vanilla;

            SettingsManager.Save();

            MainContent.Content = new VanillaPage();
        }

        // Modpacks button click event
        private void ModpacksButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.Modpacks;

            SettingsManager.Save();

            MainContent.Content = new ModpacksPage();
        }

        // Configs button click event
        private void ConfigsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Settings.LastPage = LauncherPage.Configs;

            SettingsManager.Save();

            MainContent.Content = new ConfigsPage();
        }

        // Window closing event
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            JavaRuntimeManager.CleanupDeletedFolder();

            MinecraftVersionManager.CleanupDeletedFolder();
        }

        // Player name text box text changed event
        private void PlayerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string name = PlayerNameTextBox.Text;

            SettingsManager.Settings.PlayerName = name;

            SettingsManager.Save();

            bool isEmpty = string.IsNullOrWhiteSpace(name);

            PlayerNameWarning.Visibility = isEmpty
                ? Visibility.Visible
                : Visibility.Collapsed;

            bool hasInvalidChars = !Regex.IsMatch(name, @"^[a-zA-Z0-9_]*$");

            PlayerNameAttention.Visibility = hasInvalidChars
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Player name text box lost focus event
        private void PlayerNameTextBox_LostFocus(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        // Root grid mouse down event
        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }
    }
}