using Newtonsoft.Json;

namespace ServerLauncher;

public class Config
{
    [JsonIgnore]
    private static string Path { get; } =
        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ServerLauncher", "config.json");

    public Server[] Servers { get; set; } = Array.Empty<Server>();

    public Dictionary<string, string> LaunchArgs { get; set; } = new Dictionary<string, string>();

    public string? AppDataPath { get; set; } = null;


    public static Config Load()
    {
        if (!File.Exists(Path))
        {
            var config = new Config();
            config.Save();
            return config;
        }

        return JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path));
    }

    public void Save()
    {
        if (!Directory.Exists(System.IO.Path.GetDirectoryName(Path)))
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));

        File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}

public class Server
{
    public string Name { get; set; } = "Default Server";
    public ushort Port { get; set; } = 7777;
    public Dictionary<string, string> LaunchArgs { get; set; } = new Dictionary<string, string>();

    public string? AppDataPath { get; set; } = null;
    
    public bool IncludeInLaunchAll { get; set; } = true;

    public string GetLaunchArgsString()
    {
        // combine the global launch args with the server specific ones
        var launchArgs = new Dictionary<string, string>(Program.Config.LaunchArgs);
        foreach (var (key, value) in LaunchArgs)
        {
            launchArgs[key] = value;
        }

        return string.Join(" ", launchArgs.Select(x => $"{x.Key} {x.Value}"));
    }

    public string? GetAppDataPath()
    {
        return string.IsNullOrWhiteSpace(AppDataPath) ? null : Path.GetFullPath(AppDataPath);
    }
}