using AsLauncher.Core;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace AsLauncher.Models
{
    public class MinecraftVersionEntry : INotifyPropertyChanged
    {
        // Install State variable
        private MinecraftVersionInstallState _installState = MinecraftVersionInstallState.NotInstalled;

        // Minecraft Version Entry Properties
        public string Id { get; set; } = "";

        public string Url { get; set; } = "";

        public MinecraftVersionType Type { get; set; }

        // CancellationTokenSource for download cancellation
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        // Install State property with change notification
        public MinecraftVersionInstallState InstallState
        {
            get => _installState;

            set
            {
                if (_installState == value)
                    return;

                _installState = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstallState)));
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;


        // Download ProgressBar
        private double _downloadProgress;

        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadProgress)));
            }
        }

        private Visibility _isDownloadProgressVisible = Visibility.Collapsed;

        public Visibility IsDownloadProgressVisible
        {
            get => _isDownloadProgressVisible;
            set
            {
                _isDownloadProgressVisible = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDownloadProgressVisible)));
            }
        }

        // Setup ProgressBar
        private double _setupProgress;

        public double SetupProgress
        {
            get => _setupProgress;
            set
            {
                _setupProgress = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SetupProgress)));
            }
        }

        private Visibility _isSetupProgressVisible = Visibility.Collapsed;

        public Visibility IsSetupProgressVisible
        {
            get => _isSetupProgressVisible;
            set
            {
                _isSetupProgressVisible = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSetupProgressVisible)));
            }
        }

        // Status Text
        private string _setupStatus = "";

        public string SetupStatus
        {
            get => _setupStatus;
            set
            {
                _setupStatus = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SetupStatus)));
            }
        }

        // ProgressBar Brush
        private Brush _progressBarBrush = Theme.LightBlue;

        public Brush ProgressBarBrush
        {
            get => _progressBarBrush;
            set
            {
                _progressBarBrush = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressBarBrush)));
            }
        }
    }
}