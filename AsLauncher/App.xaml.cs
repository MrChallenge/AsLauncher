using System;
using System.Windows;
using System.Runtime.InteropServices;

namespace AsLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Startup Crash");
            }
        }
        /*
        public App()
        {
            AllocConsole();
        }
        */
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}