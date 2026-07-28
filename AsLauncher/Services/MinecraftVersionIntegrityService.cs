using System.IO;
using System.Security.Cryptography;

namespace AsLauncher.Services
{
    public static class MinecraftVersionIntegrityService
    {
        // Hash scan (SHA1)
        public static string ComputeFileSha1(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);

            using SHA1 sha1 = SHA1.Create();

            byte[] hash = sha1.ComputeHash(stream);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool VerifyFile(string filePath, string expectedSha1)
        {
            if (!File.Exists(filePath))
                return false;

            string actualSha1 = ComputeFileSha1(filePath);

            return actualSha1.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ValidateIntegrity(string versionId)
        {
            if (!ValidateClient(versionId))
                return false;

            if (!ValidateLibraries(versionId))
                return false;

            if (!ValidateAssets(versionId))
                return false;

            return true;
        }

        private static bool ValidateClient(string versionId)
        {
            return true;
        }

        private static bool ValidateLibraries(string versionId)
        {
            return true;
        }

        private static bool ValidateAssets(string versionId)
        {
            return true;
        }
    }
}
