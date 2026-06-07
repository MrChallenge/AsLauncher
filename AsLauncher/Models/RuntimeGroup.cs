namespace AsLauncher.Models
{
    public class RuntimeGroup
    {
        public string MinecraftVersion { get; set; } = "";

        public List<JavaRuntimeEntry> Runtimes { get; set; } = new();
    }
}