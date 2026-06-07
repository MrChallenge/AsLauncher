using AsLauncher.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace AsLauncher.Services
{
    public static class RuntimeRegistryService
    {
        public static RuntimeRegistry Load()
        {
            string json = File.ReadAllText("Data/runtimes.json");

            return JsonSerializer.Deserialize<RuntimeRegistry>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
                ?? new RuntimeRegistry();
        }
    }
}