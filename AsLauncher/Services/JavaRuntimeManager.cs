using System.IO;
using AsLauncher.Models;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace AsLauncher.Services
{
    public static class JavaRuntimeManager
    {
        // Create folders for runtimes and temp files
        public static readonly string RuntimesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Runtimes");

        public static readonly string TempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");

        public static readonly string ExtractedRuntimesFolder = Path.Combine(TempFolder, "ExtractedRuntimes");

        public static readonly string DeletedRuntimesFolder = Path.Combine(TempFolder, "DeletedRuntimes");

        public static readonly string DownloadedRuntimesFolder = Path.Combine(TempFolder, "DownloadedRuntimes");

        // Initialize folders
        public static void Initialize()
        {
            Directory.CreateDirectory(RuntimesFolder);

            Directory.CreateDirectory(TempFolder);

            Directory.CreateDirectory(ExtractedRuntimesFolder);

            Directory.CreateDirectory(DeletedRuntimesFolder);

            Directory.CreateDirectory(DownloadedRuntimesFolder);
        }

        // Install runtime from archive
        public static async Task<bool> InstallRuntime(string archivePath, string runtimeFolderName)
        {
            string extractPath = Path.Combine(ExtractedRuntimesFolder, runtimeFolderName, "Extracted");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            ZipFile.ExtractToDirectory(archivePath, extractPath);

            string[] directories = Directory.GetDirectories(extractPath);

            if (directories.Length == 0)
            {
                CleanupTemp(runtimeFolderName);

                return false;
            }
            string runtimeRoot = directories[0];

            if (!JavaRuntimeValidator.IsValidRuntime(runtimeRoot))
            {
                CleanupTemp(runtimeFolderName);

                return false;
            }
            string finalPath = Path.Combine(RuntimesFolder, runtimeFolderName);

            if (Directory.Exists(finalPath))
            {
                Directory.Delete(finalPath, true);
            }

            Directory.Move(runtimeRoot, finalPath);

            Directory.Delete(extractPath, true);

            CleanupTemp(runtimeFolderName);

            return true;
        }

        // Check if runtime is installed
        public static bool IsRuntimeInstalled(string folderName)
        {
            string runtimePath = Path.Combine(RuntimesFolder, folderName);

            return
                JavaRuntimeValidator.IsValidRuntime(runtimePath);
        }

        // Download runtime archive with progress reporting
        public static async Task<string?> DownloadRuntime(
            string downloadUrl,
            string archiveName,
            CancellationToken cancellationToken,
            Action<double>? progressCallback = null)
        {
            string archivePath = Path.Combine(DownloadedRuntimesFolder, archiveName);

            using HttpClient client = new();

            using HttpResponseMessage response = await client.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            await using FileStream fileStream = new FileStream(
                archivePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                8192,
                true);

            byte[] buffer = new byte[8192];

            long totalRead = 0;

            int read;

            while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);

                totalRead += read;

                if (totalBytes.HasValue)
                {
                    double progress = (double)totalRead / totalBytes.Value * 100;

                    progressCallback?.Invoke(progress);
                }
            }

            return archivePath;
        }

        // Mark runtime as deleted by moving it to temp folder
        public static bool DeleteRuntime(string runtimeFolderName)
        {
            string runtimePath = Path.Combine(RuntimesFolder, runtimeFolderName);

            if (!Directory.Exists(runtimePath))
                return false;

            Directory.CreateDirectory(DeletedRuntimesFolder);

            string deletedPath = Path.Combine(DeletedRuntimesFolder, runtimeFolderName + ".deleted");

            if (Directory.Exists(deletedPath))
            {
                Directory.Delete(deletedPath, true);
            }

            Directory.Move(runtimePath, deletedPath);

            return true;
        }

        // Restore deleted runtime by moving it back to runtimes folder
        public static bool RestoreRuntime(string runtimeFolderName)
        {
            string deletedPath = Path.Combine(DeletedRuntimesFolder, runtimeFolderName + ".deleted");

            if (!Directory.Exists(deletedPath))
                return false;

            string runtimePath = Path.Combine(RuntimesFolder, runtimeFolderName);

            if (Directory.Exists(runtimePath))
            {
                Directory.Delete(runtimePath, true);
            }

            Directory.Move(deletedPath, runtimePath);

            return true;
        }

        // Check if runtime is marked as deleted
        public static bool IsRuntimeDeleted(string runtimeFolderName)
        {
            string deletedPath = Path.Combine(DeletedRuntimesFolder, runtimeFolderName + ".deleted");

            return Directory.Exists(deletedPath);
        }

        // Get runtime installation state
        public static JavaRuntimeInstallState GetRuntimeState(string runtimeFolder)
        {
            if (IsRuntimeInstalled(runtimeFolder))
            {
                return JavaRuntimeInstallState.Installed;
            }

            if (IsRuntimeDeleted(runtimeFolder))
            {
                return JavaRuntimeInstallState.Removed;
            }

            return JavaRuntimeInstallState.NotInstalled;
        }

        // Cleanup temp files related to runtime
        public static void CleanupTemp(string runtimeFolderName)
        {
            string archivePath = Path.Combine(DownloadedRuntimesFolder, runtimeFolderName + ".zip");

            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            string extractedPath = Path.Combine(ExtractedRuntimesFolder, runtimeFolderName);

            if (Directory.Exists(extractedPath))
            {
                Directory.Delete(extractedPath, true);
            }
        }

        // Cleanup deleted runtimes that are still in temp folder
        public static void CleanupDeletedFolder()
        {
            if (!Directory.Exists(DeletedRuntimesFolder))
            {
                return;
            }

            foreach (string directory in Directory.GetDirectories(DeletedRuntimesFolder))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {

                }
            }
        }
    }
}