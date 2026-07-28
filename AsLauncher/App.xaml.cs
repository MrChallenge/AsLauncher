using AsLauncher.Core.Logger;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace AsLauncher
{
    public partial class App : Application
    {
        // Allocates console for debugging purposes
        public App()
        {
            if (!AllocConsole())
            {
                // W.I.P
            }
        }

        // Called when app is starting up
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                Logger.Initialize();

                Logger.Info(LoggerConfig.Launcher, LoggerConfig.AppStarted);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                MessageBox.Show(ex.ToString(), LoggerConfig.StartupCrash);
            }
        }

        // Called when app is exiting
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info(LoggerConfig.Launcher, LoggerConfig.AppClosed);

            Logger.Shutdown();

            base.OnExit(e);
        }

        // Allocates new console for current process
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}