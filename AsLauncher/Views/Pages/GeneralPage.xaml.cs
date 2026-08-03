using AsLauncher.Core.Logger;
using System.Windows.Controls;
using System.Windows.Input;

namespace AsLauncher.Views.Pages
{
    public partial class GeneralPage : UserControl
    {
        public GeneralPage()
        {
            InitializeComponent();
        }

        private void LauncherLogo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Logger.PrintColorPalette();
        }
    }
}