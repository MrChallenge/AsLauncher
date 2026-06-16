using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace AsLauncher.Models
{
    public class JavaRuntimeEntry : INotifyPropertyChanged
    {
        private JavaRuntimeInstallState _installState = JavaRuntimeInstallState.NotInstalled;

        private double _progress;

        public double Progress
        {
            get => _progress;

            set
            {
                _progress = value;

                OnPropertyChanged(nameof(Progress));
            }
        }

        private Visibility _isProgressVisible = Visibility.Collapsed;

        public Visibility IsProgressVisible
        {
            get => _isProgressVisible;

            set
            {
                _isProgressVisible = value;

                OnPropertyChanged(nameof(IsProgressVisible));
            }
        }
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public string Name { get; set; } = "";

        public string RuntimeFolder { get; set; } = "";

        public string DownloadUrl { get; set; } = "";

        public string Description { get; set; } = "";

        public JavaRuntimeInstallState InstallState
        {
            get => _installState;

            set
            {
                _installState = value;

                OnPropertyChanged(nameof(InstallState));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}