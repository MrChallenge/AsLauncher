using AsLauncher.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace AsLauncher.Services
{
    public static class JavaRuntimeRegistryService
    {
        public static JavaRuntimeRegistry Load()
        {
            string json = File.ReadAllText("Data/runtimes.json");

            return JsonSerializer.Deserialize<JavaRuntimeRegistry>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
                ?? new JavaRuntimeRegistry();
        }
    }
}