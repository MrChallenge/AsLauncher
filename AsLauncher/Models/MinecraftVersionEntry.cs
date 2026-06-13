using System.ComponentModel;

namespace AsLauncher.Models
{
    public class MinecraftVersionEntry : INotifyPropertyChanged
    {
        private MinecraftVersionInstallState _installState = MinecraftVersionInstallState.NotInstalled;

        public string Id { get; set; } = "";

        public string Type { get; set; } = "";

        public string Url { get; set; } = "";

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
    }
}