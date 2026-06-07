using AsLauncher.Views.Pages;
using System.Windows;
using System.Windows.Media;
using AsLauncher.Services;

namespace AsLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            UpdateSidebarState();

            RuntimeManager.Initialize();

            RuntimeManager.CleanupDeletedFolder();

            MainContent.Content = new GeneralPage();
        }
       
        private bool _sidebarCollapsed = false;

        private void UpdateSidebarState()
        {
            if (_sidebarCollapsed)
            {
                SidebarColumn.Width = new GridLength(72);

                SidebarTitle.Text = "As";

                NavGeneralText.Visibility = Visibility.Collapsed;
                NavModpacksText.Visibility = Visibility.Collapsed;
                NavConfigsText.Visibility = Visibility.Collapsed;

                SidebarToggleIcon.RenderTransform = new RotateTransform(0);
            }
            else
            {
                SidebarColumn.Width = new GridLength(240);

                SidebarTitle.Text = "AsLauncher";

                NavGeneralText.Visibility = Visibility.Visible;
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
            MainContent.Content = new GeneralPage();
        }

        private void ModpacksButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ModpacksPage();
        }

        private void ConfigsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ConfigsPage();
        }
    }
}