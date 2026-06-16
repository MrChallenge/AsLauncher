using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace AsLauncher.Models
{
    public class MinecraftVersionEntry : INotifyPropertyChanged
    {
        private MinecraftVersionInstallState _installState = MinecraftVersionInstallState.NotInstalled;

        public string Id { get; set; } = "";

        public string Type { get; set; } = "";

        public string Url { get; set; } = "";

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public MinecraftVersionInstallState InstallState
        {
            get => _installState;

            set
            {
                _installState = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InstallState)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private double _progress;

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
            }
        }

        private Visibility _isProgressVisible = Visibility.Collapsed;

        public Visibility IsProgressVisible
        {
            get => _isProgressVisible;
            set
            {
                _isProgressVisible = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProgressVisible)));
            }
        }
    }
}