using Spectre.Console;

namespace ServerLauncher.IO;



internal static class PathManager
{
    private static bool _configDirOverride;

    internal static readonly string GameUserDataRoot;
    internal static readonly string ConfigDirectoryPath;
    internal static readonly string ConfigPath;

    internal static bool CorrectPathFound { get; private set; }

    static PathManager()
    {
        ProcessHostPolicy();

        GameUserDataRoot = _configDirOverride
            ? "AppData" + Path.DirectorySeparatorChar
            : GetSpecialFolderPath() + "SCP Secret Laboratory" + Path.DirectorySeparatorChar;

        ConfigDirectoryPath = $"{GameUserDataRoot}config{Path.DirectorySeparatorChar}";

        ConfigPath = ConfigDirectoryPath + "config_serverlauncher.json";
    }

    private static string GetSpecialFolderPath()
    {
        try
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (!string.IsNullOrWhiteSpace(path))
            {
                CorrectPathFound = true;
                return path + Path.DirectorySeparatorChar;
            }

            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (!string.IsNullOrWhiteSpace(path))
            {
                CorrectPathFound = true;

                if (OperatingSystem.IsLinux())
                    return path + Path.DirectorySeparatorChar + ".config" + Path.DirectorySeparatorChar;

                if (OperatingSystem.IsWindows())
                    return path + Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Roaming" + Path.DirectorySeparatorChar;

                AnsiConsole.MarkupLine("Failed to get special folder path - unsupported platform!");
                throw new PlatformNotSupportedException();
            }

            CorrectPathFound = false;
            AnsiConsole.MarkupLine($"Failed to get special folder path - it's always null or empty!");

            return string.Empty;
        }
        catch (Exception e)
        {
            CorrectPathFound = false;
            AnsiConsole.MarkupLine($"Failed to get special folder path! Exception: {e.Message}");

            throw;
        }
    }

    private static void ProcessHostPolicy()
    {
        try
        {
            _configDirOverride = false;

            if (!File.Exists("hoster_policy.txt"))
                return;

            var lines = File.ReadAllLines("hoster_policy.txt");

            foreach (var l in lines)
            {
                if (!l.Contains("gamedir_for_configs: true", StringComparison.OrdinalIgnoreCase))
                    continue;

                _configDirOverride = true;
                AnsiConsole.MarkupLine("Applied policy: gamedir_for_configs: true");
                break;
            }
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"Failed to process hoster_policy.txt file: {e.Message}");
        }
    }
}