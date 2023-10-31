using Newtonsoft.Json;

namespace ServerLauncher;

public class Config
{
    [JsonIgnore]
    private static string Path { get; } =
        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ServerLauncher", "config.json");
    public Server[] Servers { get; set; } = Array.Empty<Server>();

    public string[] LaunchArgs { get; set; } = Array.Empty<string>();
    
    public string AppDataPath { get; set; } = string.Empty;


    public static Config Load()
    {
        if (!File.Exists(Path))
        {
            var config = new Config();
            config.Save();
            return config;
        }

        return Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path));
    }

    public void Save()
    {
        if (!Directory.Exists(System.IO.Path.GetDirectoryName(Path)))
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        
        File.WriteAllText(Path, Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
    }
}

public class Server
{
    public string Name { get; set; } = "Default Server";
    public ushort Port { get; set; } = 7777;
    public string[] LaunchArgs { get; set; } = Array.Empty<string>();
    
    private string AppDataPath { get; set; } = string.Empty;
    
    public string GetLaunchArgsString()
    {
        return string.Join(" ", Program.Config.LaunchArgs) + " " + string.Join(" ", LaunchArgs);
    }
    
    public string? GetAppDataPath()
    {
        return string.IsNullOrWhiteSpace(AppDataPath) ? null : Path.GetFullPath(AppDataPath);
    }
    
}