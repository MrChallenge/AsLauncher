namespace AsLauncher.Models
{
    public class JavaRuntimeGroup
    {
        public string MinecraftVersion { get; set; } = "";

        public List<JavaRuntimeEntry> JavaRuntimes { get; set; } = new();
    }
}