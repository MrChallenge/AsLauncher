using AsLauncher.Core;
using AsLauncher.Core.Logger;
using AsLauncher.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace AsLauncher.Services
{
    public static class MinecraftLaunchManager
    {
        // Get Main class from <version>.json
        public static string GetMainClass(string versionId)
        {
            using JsonDocument document = MinecraftVersionManager.LoadVersionJson(versionId);

            if (document.RootElement.TryGetProperty("mainClass", out JsonElement mainClass))
            {
                return mainClass.GetString()!;
            }

            if (document.RootElement.TryGetProperty("inheritsFrom", out JsonElement inherited))
            {
                return GetMainClass(inherited.GetString()!);
            }

            throw new Exception($"mainClass not found for version {versionId}");
        }

        // Get Assets dir path
        public static string GetAssetsDir()
        {
            return MinecraftVersionManager.AssetsFolder;
        }

        // Get Game dir path
        public static string GetGameDir()
        {
            return MinecraftVersionManager.MinecraftFolder;
        }

        // Build JVM arguments
        public static string BuildJvmArguments(string versionId, MinecraftAccount account)
        {
            using JsonDocument document = MinecraftVersionManager.LoadVersionJson(versionId);

            if (!document.RootElement.TryGetProperty("arguments", out JsonElement arguments))
            {
                return "";
            }

            return string.Join(" ", ParseArguments(arguments.GetProperty("jvm"), versionId, true, account));
        }

        // Build game arguments
        public static string BuildGameArguments(string versionId, MinecraftAccount account)
        {
            using JsonDocument document = MinecraftVersionManager.LoadVersionJson(versionId);

            if (!document.RootElement.TryGetProperty("arguments", out JsonElement arguments))
            {
                return "";
            }

            return string.Join(" ", ParseArguments(arguments.GetProperty("game"), versionId, false, account));
        }

        // Parse arguments from JSON
        private static List<string> ParseArguments(JsonElement arguments, string versionId, bool isJvm, MinecraftAccount account)
        {
            List<string> result = new();

            foreach (JsonElement arg in arguments.EnumerateArray())
            {
                if (arg.ValueKind == JsonValueKind.String)
                {
                    string value = arg.GetString()!;

                    if (isJvm)
                    {
                        if (value == "-XstartOnFirstThread")
                            continue;

                        if (value.Contains("java-objc-bridge"))
                            continue;

                        if (value == "-cp" || value == "${classpath}")
                            continue;

                        if (value.StartsWith("-Dos.name="))
                            continue;

                        if (value.StartsWith("-Dos.version="))
                            continue;
                    }

                    result.Add(ReplaceVariables(value, versionId, account));
                }
                else if (arg.ValueKind == JsonValueKind.Object)
                {
                    if (arg.TryGetProperty("rules", out JsonElement rules))
                    {
                        if (!IsRuleAllowed(rules))
                            continue;
                    }

                    if (!arg.TryGetProperty("value", out JsonElement value))
                        continue;

                    if (value.ValueKind == JsonValueKind.String)
                    {
                        string argument = value.GetString()!;

                        if (isJvm)
                        {
                            if (argument.StartsWith("-Dos.name="))
                                continue;

                            if (argument.StartsWith("-Dos.version="))
                                continue;
                        }

                        result.Add(ReplaceVariables(argument, versionId, account));
                    }
                    else if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in value.EnumerateArray())
                        {
                            string argument = item.GetString()!;

                            if (isJvm)
                            {
                                if (argument.StartsWith("-Dos.name="))
                                    continue;

                                if (argument.StartsWith("-Dos.version="))
                                    continue;
                            }

                            result.Add(ReplaceVariables(argument, versionId, account)
                            );
                        }
                    }
                }
            }

            return result;
        }

        // Load resolved version JSON
        //public static JsonDocument LoadResolvedVersionJson(string versionId);

        // Check if rules allow launching on current OS
        private static bool IsRuleAllowed(JsonElement rules)
        {
            bool? allowed = null;

            foreach (JsonElement rule in rules.EnumerateArray())
            {
                string action = rule.GetProperty("action").GetString()!;

                bool matches = true;

                if (rule.TryGetProperty("os", out JsonElement os))
                {
                    if (os.TryGetProperty("name", out JsonElement name))
                    {
                        string osName = name.GetString()!;

                        matches = (osName == "windows" && OperatingSystem.IsWindows()) ||
                                  (osName == "linux" && OperatingSystem.IsLinux()) ||
                                  (osName == "osx" && OperatingSystem.IsMacOS());
                    }
                }

                if (rule.TryGetProperty("features", out JsonElement features))
                {
                    foreach (JsonProperty feature in features.EnumerateObject())
                    {
                        switch (feature.Name)
                        {
                            case "is_demo_user": matches &= MinecraftLaunchOptions.DemoMode;
                                break;

                            case "has_custom_resolution": matches &= MinecraftLaunchOptions.CustomResolution;
                                break;

                            default: matches = false;
                                break;
                        }
                    }
                }

                if (!matches)
                {
                    continue;
                }

                allowed = action == "allow";
            }

            return allowed ?? false;
        }

        // Build Java Agent argument
        private static string BuildJavaAgentArgument()
        {
            string? javaAgent = Directory.GetFiles(AppContext.BaseDirectory, "AsLauncher_JavaAgent-*.jar")
                                         .Select(Path.GetFileName)
                                         .FirstOrDefault();

            if (javaAgent == null)
                return "";

            string agentPath = Path.Combine(AppContext.BaseDirectory, javaAgent);

            return $"-javaagent:\"{agentPath}\"";
        }

        // Launch Minecraft with specified version
        public static async Task LaunchAsync(string versionId)
        {
            Logger.Info(LoggerConfig.JavaSource, $"========== Launching Minecraft {versionId} ==========");

            int javaVersion = MinecraftVersionManager.GetRequiredJavaVersion(versionId);

            Logger.Debug(LoggerConfig.JavaSource, $"Required Java version: {javaVersion}");

            JavaRuntimeEntry? runtime = JavaRuntimeManager.GetRuntimeForJavaVersion(javaVersion);

            if (runtime == null)
            {
                Logger.Error(LoggerConfig.JavaSource, $"Java {javaVersion} runtime not found.");
                MessageBox.Show($"Java {javaVersion} not installed.");
                return;
            }

            Logger.Debug(LoggerConfig.JavaSource, $"Java runtime: {runtime.RuntimeFolder}");

            string? javaPath = JavaRuntimeManager.GetJavawExecutable(runtime.RuntimeFolder);

            if (javaPath == null)
            {
                Logger.Error(LoggerConfig.JavaSource, $"javaw.exe not found in runtime.");
                MessageBox.Show("Not found javaw.exe");
                return;
            }

            Logger.Debug(LoggerConfig.JavaSource, $"Java executable: {javaPath}");

            Logger.Info(LoggerConfig.VersionsSource, "Checking Minecraft integrity...");

            if (!await MinecraftVersionIntegrityService.EnsureIntegrityAsync(versionId))
            {
                Logger.Error(LoggerConfig.VersionsSource, "Integrity check failed. Launch aborted.");
                return;
            }

            Logger.Success(LoggerConfig.VersionsSource, "Integrity check completed.");

            MinecraftAccount account = MinecraftAccountManager.GetCurrentAccount();

            Logger.Debug(LoggerConfig.JavaSource, $"Account: {account.UserName}");

            string classPath = MinecraftVersionManager.BuildClassPath(versionId);

            Logger.Debug(LoggerConfig.JavaSource, "Classpath built.");

            string mainClass = GetMainClass(versionId);

            Logger.Debug(LoggerConfig.JavaSource, $"Main class: {mainClass}");

            string jvmArguments = BuildJvmArguments(versionId, account);

            Logger.Debug(LoggerConfig.JavaSource, "JVM arguments built.");

            string gameArguments = BuildGameArguments(versionId, account);

            Logger.Debug(LoggerConfig.JavaSource, "Game arguments built.");

            string javaAgentArgument = BuildJavaAgentArgument();

            if (!string.IsNullOrEmpty(javaAgentArgument))
            {
                jvmArguments += $" {javaAgentArgument}";

                Logger.Debug(LoggerConfig.JavaSource, "Java Agent: enabled.");
            }
            else
            {
                Logger.Debug(LoggerConfig.JavaSource, "Java Agent: disabled.");
            }

            string finalCommand = $"{jvmArguments} -cp \"{classPath}\" {mainClass} {gameArguments}";

            File.WriteAllText("launch.txt", finalCommand);

            Logger.Debug(LoggerConfig.JavaSource, "Launch command built.");

            ProcessStartInfo startInfo = new()
            {
                FileName = javaPath,
                Arguments = finalCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            Logger.Info(LoggerConfig.JavaSource, "Starting Minecraft process...");

            Process process = Process.Start(startInfo)!;

            Logger.Success(LoggerConfig.JavaSource, $"Minecraft started. PID={process.Id}");

            // Minecraft output
            string output = await process.StandardOutput.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Logger.Debug(LoggerConfig.JavaSource, output);
            }

            string error = await process.StandardError.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Logger.Error(LoggerConfig.JavaSource, error);
            }

            Logger.Debug(LoggerConfig.JavaSource, $"Minecraft process output streams closed. PID={process.Id}");
        }

        // Replace variables in argument string with actual values
        private static string ReplaceVariables(string argument, string versionId, MinecraftAccount account)
        {
            string gameDir = GetGameDir();

            string assetsDir = GetAssetsDir();

            string assetIndex = MinecraftVersionManager.GetAssetIndexId(versionId);

            string versionType = MinecraftVersionManager.GetVersionType(versionId);

            string nativesDir = Path.Combine(MinecraftVersionManager.VersionsFolder, versionId, "natives");

            string startupWidth = Theme.StartupWidth;

            string startupHeight = Theme.StartupHeight;

            argument = argument.Replace("${version_name}", versionId);
            argument = argument.Replace("${game_directory}", gameDir);
            argument = argument.Replace("${assets_root}", assetsDir);
            argument = argument.Replace("${assets_index_name}", assetIndex);
            argument = argument.Replace("${natives_directory}", nativesDir);

            argument = argument.Replace("${launcher_name}", "AsLauncher");
            argument = argument.Replace("${launcher_version}", "1.0");

            argument = argument.Replace("${auth_player_name}", account.UserName);
            argument = argument.Replace("${auth_uuid}", account.Uuid);
            argument = argument.Replace("${auth_access_token}", account.AccessToken);
            argument = argument.Replace("${user_type}", account.UserType);
            argument = argument.Replace("${user_properties}", "{}");

            argument = argument.Replace("${clientid}", "0");
            argument = argument.Replace("${auth_xuid}", "0");

            argument = argument.Replace("${version_type}", versionType);

            argument = argument.Replace("${resolution_width}", startupWidth);
            argument = argument.Replace("${resolution_height}", startupHeight);

            return argument;
        }
    }
}