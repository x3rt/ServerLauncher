using Newtonsoft.Json;

namespace ServerLauncher.IO;

public class Config
{
    [JsonIgnore]
    private static string Path { get; } =
        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ServerLauncher", "config.json");

    public Server[] Servers { get; set; } = Array.Empty<Server>();

    public SharedOptions GlobalOptions { get; } = new();

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
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public class Server
    {
        public string Name { get; set; } = "Default Server";
        public ushort Port { get; set; } = 7777;

        public SharedOptions Options { get; } = new();

        public bool IncludeInLaunchAll { get; set; } = true;
    }
}

public class SharedOptions
{
    [JsonIgnore] private string? _appDataPath;
    public Dictionary<string, string> LaunchArgs { get; set; } = new();

    public string? AppDataPath
    {
        get => _appDataPath;
        set => _appDataPath = string.IsNullOrWhiteSpace(value) ? null : value;
    }
}