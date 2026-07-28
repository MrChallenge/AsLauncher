using AsLauncher.Models;
using System.IO;
using System.Text.Json;

namespace AsLauncher.Services
{
    public static class JavaRuntimeRegistryService
    {
        // Read json file and deserialize it into JavaRuntimeRegistry object
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