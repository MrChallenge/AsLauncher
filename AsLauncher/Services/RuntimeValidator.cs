using System.IO;

namespace AsLauncher.Services
{
    public static class RuntimeValidator
    {
        public static bool IsValidRuntime(string path)
        {
            string javaExe = Path.Combine(path, "bin", "java.exe");

            string releaseFile = Path.Combine(path, "release");

            return
                File.Exists(javaExe) &&
                File.Exists(releaseFile);
        }
    }
}