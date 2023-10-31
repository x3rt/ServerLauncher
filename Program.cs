using System.Diagnostics;
using Spectre.Console;

namespace ServerLauncher;

public static class Program
{
    public static Config Config { get; set; } = Config.Load();

    private static bool _exit;

    public static void Main(string[] args)
    {
        while (!_exit)
        {
            MainMenu();
        }
    }

    private static void MainMenu()
    {
        // AnsiConsole.Clear();
        Config = Config.Load();
        Config.Save();

        var choices = new Dictionary<string, Action>();
        choices.Add("Start All Servers", StartAllServers);
        choices.Add("Start Specific Server", SpecificServerMenu);
        choices.Add("Edit Server", EditServerMenu);
        choices.Add("Edit Global Settings", EditGlobalSettingsMenu);
        choices.Add("Exit", Exit);

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));

        // AnsiConsole.Clear();
        choices[action].Invoke();
    }

    private static void Exit()
    {
        _exit = true;
        Environment.Exit(0);
    }

    private static void EditGlobalSettingsMenu()
    {
        // Display The Current Settings in a prompt and allow the user to select which one to edit

        var choices = new Dictionary<string, Action>();
        choices.Add(
            $"[bold]App Data Path:[/] [green]{(string.IsNullOrWhiteSpace(Config.AppDataPath) ? "Default" : Config.AppDataPath)}[/]",
            EditGlobalAppDataPath);
        choices.Add($"[bold]Launch Args:[/] [green]{Config.LaunchArgs.Count}[/]", EditGlobalLaunchArgs);

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which setting would you like to edit?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));
        // AnsiConsole.Clear();
        choices[action].Invoke();
    }

    private static void EditGlobalAppDataPath()
    {
        AnsiConsole.MarkupLine(
            $"[bold]Current App Data Path:[/] [green]{(string.IsNullOrWhiteSpace(Config.AppDataPath) ? "Default" : Config.AppDataPath)}[/]");
        if (AnsiConsole.Confirm("Would you like to change the app data path?"))
        {
            Config.AppDataPath = AnsiConsole.Prompt(
                new TextPrompt<string?>("What is the new app data path?")
                    .AllowEmpty());
            Config.Save();
        }
    }

    private static void EditGlobalLaunchArgs()
    {
        // Display the current launch args in a prompt and allow the user to select which one to edit, or add a new one

        var choices = new Dictionary<string, Action>();
        foreach (var (key, value) in Config.LaunchArgs)
        {
            choices.Add($"[bold]{key}[/] [green]{value}[/]", () => EditGlobalLaunchArg(key));
        }

        choices.Add("[bold]Add New Launch Arg[/]", AddGlobalLaunchArg);

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which launch arg would you like to edit?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));

        choices[action].Invoke();

        // AnsiConsole.Clear();
    }

    private static void AddGlobalLaunchArg()
    {
        var key = AnsiConsole.Prompt(
            new TextPrompt<string>("What is the launch arg key?"));

        var value = AnsiConsole.Prompt(
            new TextPrompt<string>("What is the launch arg value?"));

        Config.LaunchArgs.Add(key, value);
        Config.Save();
    }

    private static void EditGlobalLaunchArg(string key)
    {
        var leave = false;
        // Display the current key and value
        while (!leave)
        {
            AnsiConsole.MarkupLine($"[bold]Current Key:[/] [green]{key}[/]");
            AnsiConsole.MarkupLine($"[bold]Current Value:[/] [green]{Config.LaunchArgs[key]}[/]");
            AnsiConsole.WriteLine();

            // Ask if the user wants to delete the launch arg or edit the value
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .PageSize(10)
                    .AddChoices(new[] { "Edit Value", "Edit Key", "Delete", "Back" }));

            switch (action)
            {
                case "Edit Value":
                    Config.LaunchArgs[key] = AnsiConsole.Prompt(
                        new TextPrompt<string>("What is the new value?"));
                    Config.Save();
                    break;
                case "Edit Key":
                    var newKey = AnsiConsole.Prompt(
                        new TextPrompt<string>("What is the new key?"));
                    Config.LaunchArgs.Add(newKey, Config.LaunchArgs[key]);
                    Config.LaunchArgs.Remove(key);
                    Config.Save();
                    break;
                case "Delete":
                    leave = true;
                    Config.LaunchArgs.Remove(key);
                    Config.Save();
                    break;
                default:
                    leave = true;
                    break;
            }

            // AnsiConsole.Clear();
        }
    }

    private static void AddServerMenu()
    {
        var server = new Server
        {
            Name = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the name of the server?")),
            Port = AnsiConsole.Prompt(
                new TextPrompt<ushort>("What is the port of the server?").DefaultValue<ushort>(7777)),
            IncludeInLaunchAll =
                AnsiConsole.Confirm("Would you like to include this server in the launch all command?"),
        };
        var launchArgs = new Dictionary<string, string>();
        while (AnsiConsole.Confirm("Would you like to add a launch arg?"))
        {
            // Get the launch arg key
            var key = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the launch arg key?"));
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the launch arg value?"));

            launchArgs.Add(key, value);
        }

        server.LaunchArgs = launchArgs;

        if (AnsiConsole.Confirm("Does this server have a custom app data path?"))
        {
            server.AppDataPath = AnsiConsole.Prompt(
                new TextPrompt<string?>("What is the app data path?").AllowEmpty());
        }

        // Display the server info
        AnsiConsole.WriteLine();

        DisplayServerInfo(server);

        AnsiConsole.WriteLine();


        if (!AnsiConsole.Confirm("Is this correct?"))
            return;

        Config.Servers = Config.Servers.Append(server).ToArray();
        Config.Save();
    }

    private static void DisplayServerInfo(Server server)
    {
        AnsiConsole.Write(new Table().AddColumns("[grey]Option[/]", "[grey]Value[/]")
            .AddRow("[bold]Name[/]", server.Name)
            .AddRow("[bold]Port[/]", server.Port.ToString())
            .AddRow("[bold]Include In Launch All[/]", server.IncludeInLaunchAll.ToString())
            .AddRow("[bold]App Data Path[/]", server.AppDataPath ?? "Default")
            .AddRow("[bold]Launch Args[/]",
                server.LaunchArgs.Count > 0
                    ? string.Join(" ", server.LaunchArgs.Select(x => $"{x.Key} {x.Value}"))
                    : "None"));
    }


    private static void EditServerMenu()
    {
        var choices = new Dictionary<string, Action>();

        foreach (var server in Config.Servers)
        {
            choices.Add($"[bold]{server.Name}[/] - Port: [green]{server.Port}[/]", () => EditServer(server));
        }

        choices.Add("[bold]Add New Server[/]", AddServerMenu);

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which server would you like to edit?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));

        choices[action].Invoke();

        // AnsiConsole.Clear();
    }

    private static void EditServer(Server server)
    {
        var leave = false;

        while (!leave)
        {
            DisplayServerInfo(server);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .PageSize(10)
                    .AddChoices(new[]
                    {
                        "Edit Name", "Edit Port", "Toggle Include In Launch All", "Edit App Data Path",
                        "Edit Launch Args", "Delete", "Back"
                    }));

            switch (action)
            {
                case "Edit Name":
                    server.Name = AnsiConsole.Prompt(
                        new TextPrompt<string>("What is the new name?"));
                    Config.Save();
                    break;
                case "Edit Port":
                    server.Port = AnsiConsole.Prompt(
                        new TextPrompt<ushort>("What is the new port?").DefaultValue<ushort>(7777));
                    Config.Save();
                    break;
                case "Toggle Include In Launch All":
                    server.IncludeInLaunchAll = !server.IncludeInLaunchAll;
                    Config.Save();
                    break;
                case "Edit App Data Path":
                    if (AnsiConsole.Confirm("Does this server have a custom app data path?"))
                    {
                        server.AppDataPath = AnsiConsole.Prompt(
                            new TextPrompt<string?>("What is the app data path?").AllowEmpty());
                    }
                    else
                    {
                        server.AppDataPath = null;
                    }

                    Config.Save();
                    break;
                case "Edit Launch Args":
                    var launchArgs = new Dictionary<string, string>();
                    while (AnsiConsole.Confirm("Would you like to add a launch arg?"))
                    {
                        // Get the launch arg key
                        var key = AnsiConsole.Prompt(
                            new TextPrompt<string>("What is the launch arg key?"));
                        var value = AnsiConsole.Prompt(
                            new TextPrompt<string>("What is the launch arg value?"));

                        launchArgs.Add(key, value);
                    }

                    server.LaunchArgs = launchArgs;
                    Config.Save();
                    break;
                case "Delete":
                    leave = true;
                    Config.Servers = Config.Servers.Where(x => x != server).ToArray();
                    Config.Save();
                    break;
                default:
                    leave = true;
                    break;
            }

            // AnsiConsole.Clear();
        }
    }

    private static void SpecificServerMenu()
    {
        var choices = new Dictionary<string, Action>();
        
        foreach (var server in Config.Servers)
        {
            choices.Add($"[bold]{server.Name}[/] - Port: [green]{server.Port}[/]", () => StartServer(server));
        }
        
        var action = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Which server would you like to start?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));
        
        foreach (var act in action)
        {
            choices[act].Invoke();
        }

        _exit = true;
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
        // In a new window, run
        // LocalAdmin.Exe [Port] [Args]
        
        var args = new List<string>
        {
            server.Port.ToString(),
            server.GetLaunchArgsString()
        };
        
        
        var appDataPth = server.GetAppDataPath();
        if (!string.IsNullOrWhiteSpace(appDataPth))
        {
            args.Add($"-appdatapath");
            args.Add(appDataPth);
        }
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "LocalAdmin.exe",
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };
        
        process.Start();
        
    }
}