using System.Diagnostics;
using Spectre.Console;

namespace ServerLauncher;

public static class Program
{
    public static Config Config { get; set; } = Config.Load();

    private static bool _exit = false;

    public static void Main(string[] args)
    {
        while (!_exit)
        {
            MainMenu();
        }
    }

    private static void MainMenu()
    {
        Config = Config.Load();
        var serverCount = Config.Servers.Length;
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(new[] { "Start All Servers", "Start Specific Server", "Add Server", "Exit" }));

        switch (action)
        {
            case "Start All Servers":
                _exit = true;
                StartAllServers();
                break;
            case "Start Specific Server":
                SpecificServerMenu();
                break;
            case "Add Server":
                AddServerMenu();
                break;
            case "Exit":
                _exit = true;
                Environment.Exit(0);
                break;
        }
    }

    private static void AddServerMenu()
    {
        var server = new Server();
        server.Name = AnsiConsole.Ask<string>("What is the name of the server?");
        server.Port = AnsiConsole.Ask<ushort>("What is the port of the server?");
        var launchArgs = Array.Empty<string>();
        while (AnsiConsole.Confirm("Add launch arg?"))
        {
            var arg = AnsiConsole.Ask<string>("What is the launch arg?");
            launchArgs = launchArgs.Append(arg).ToArray();
        }

        server.LaunchArgs = launchArgs;
        // Display the server info
        AnsiConsole.MarkupLine($"[bold]Name:[/] {server.Name}");
        AnsiConsole.MarkupLine($"[bold]Port:[/] {server.Port}");
        AnsiConsole.MarkupLine($"[bold]Launch Args:[/] {string.Join(" ", server.LaunchArgs)}");
        if (!AnsiConsole.Confirm("Is this correct?"))
            return;

        Config.Servers = Config.Servers.Append(server).ToArray();
        Config.Save();
    }

    private static void SpecificServerMenu()
    {
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which server would you like to start?")
                .PageSize(10)
                .AddChoices(Config.Servers.Select(x => $"{x.Name} - Port: {x.Port}")));

        var server = Config.Servers.FirstOrDefault(x => $"{x.Name} - Port: {x.Port}" == action);
        if (server == null)
        {
            // Error to user
            AnsiConsole.MarkupLine("[red]Server not found![/]");
            return;
        }

        _exit = true;
        StartServer(server);
    }

    private static void StartAllServers()
    {
        foreach (var server in Config.Servers)
        {
            StartServer(server);
        }
    }

    private static void StartServer(Server server)
    {
        var process = new Process();
        process.StartInfo.FileName = "cmd.exe";

        var args = new List<string>();
        args.Add("/k");
        args.Add($"title {server.Name} - Port: {server.Port}");
        args.Add("&");
        args.Add($"LocalAdmin.exe {server.Port}");
        args.Add(server.GetLaunchArgsString());
        var appDataPth = server.GetAppDataPath();
        if (!string.IsNullOrWhiteSpace(appDataPth))
        {
            args.Add($"-appdatapath");
            args.Add(appDataPth);
        }

        process.StartInfo.Arguments = string.Join(" ", args);

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
    }
}