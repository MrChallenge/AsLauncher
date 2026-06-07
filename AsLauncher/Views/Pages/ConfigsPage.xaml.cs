using AsLauncher.Models;
using AsLauncher.Services;
using System.Collections.Generic;
using System.Windows.Controls;

namespace AsLauncher.Views.Pages
{
    public partial class ConfigsPage : UserControl
    {
        public List<RuntimeGroup> RuntimeGroups { get; }

        public ConfigsPage()
        {
            InitializeComponent();

            RuntimeGroups = RuntimeRegistryService.Load().Groups;

            DataContext = this;
        }
    }
}