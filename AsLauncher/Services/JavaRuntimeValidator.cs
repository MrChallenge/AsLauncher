using System.IO;

namespace AsLauncher.Services
{
    public static class JavaRuntimeValidator
    {
        // Check if given path is valid Java runtime by checking for existence of java.exe and release file
        public static bool IsValidRuntime(string path)
        {
            string javaExe = Path.Combine(path, "bin", "java.exe");

            string releaseFile = Path.Combine(path, "release");

            return File.Exists(javaExe) &&
                   File.Exists(releaseFile);
        }
    }
}