using ServerLauncher.IO;

namespace ServerLauncher.Core;

public static class ConfigHelper
{
    public static string? GetAppDataPath(Config.Server? server = null, bool includeGlobal = true)
    {
        return string.IsNullOrWhiteSpace(server?.Options.AppDataPath)
            ? includeGlobal ? Program.Config.GlobalOptions.AppDataPath : null
            : Path.GetFullPath(server.Options.AppDataPath);
    }

    public static string GetLaunchArgsString(Config.Server? server = null, bool includeGlobal = true)
    {
        // combine the global launch args with the server specific ones
        var launchArgs = GetLaunchArgs(server, includeGlobal);

        if (server is not null)
            foreach (var (key, value) in server.Options.LaunchArgs)
            {
                launchArgs[key] = value;
            }

        return launchArgs.Count == 0
            ? string.Empty
            : string.Join(" ", launchArgs.Select(x => $"{x.Key} {x.Value}"));
    }

    public static Dictionary<string, string?> GetLaunchArgs(Config.Server? server = null, bool includeGlobal = true)
    {
        // combine the global launch args with the server specific ones
        var launchArgs = includeGlobal ? Program.Config.GlobalOptions.LaunchArgs : new Dictionary<string, string?>();
        if (server is not null)
            foreach (var (key, value) in server.Options.LaunchArgs)
            {
                launchArgs[key] = value;
            }

        return launchArgs;
    }
}