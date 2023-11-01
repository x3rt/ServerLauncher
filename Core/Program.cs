using System.Diagnostics;
using ServerLauncher.IO;
using Spectre.Console;

namespace ServerLauncher.Core;

public static class Program
{
    public static Config Config { get; private set; } = Config.Load();

    private static readonly string LocalAdminExecutable;

    private static bool _exit;

    private const string VersionString = "2.0.0";

    static Program()
    {
        if (OperatingSystem.IsWindows())
        {
            LocalAdminExecutable = "LocalAdmin.exe";
        }
        else if (OperatingSystem.IsLinux())
        {
            LocalAdminExecutable = "LocalAdmin";
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Unsupported Operating System![/]");
            LocalAdminExecutable = string.Empty;
            Exit();
        }
    }

    public static void Main(string[] args)
    {
        Console.Title = $"Server Launcher v{VersionString}";

        while (!_exit)
        {
            MainMenu();
        }
    }

    private static void MainMenu()
    {
        AnsiConsole.Clear();
        Config = Config.Load();
        Config.Save();

        var choices = new Dictionary<string, Action>
        {
            { "Start All Servers", StartAllServers },
            { "Start Specific Server", SpecificServerMenu },
            { "Edit/Add Servers", EditServerMenu },
            { "Edit Global Settings", EditGlobalSettingsMenu },
            { "Exit", Exit }
        };

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));

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
        AnsiConsole.Clear();

        var choices = new Dictionary<string, Action>
        {
            {
                $"[bold]App Data Path:[/] [green]{(string.IsNullOrWhiteSpace(ConfigHelper.GetAppDataPath()) ? "Default" : ConfigHelper.GetAppDataPath())}[/]",
                () => EditAppDataPath()
            },
            { $"[bold]Launch Args:[/] [green]{ConfigHelper.GetLaunchArgs().Count}[/]", () => EditLaunchArgs() },
            { "[bold]Back[/]", () => { } }
        };

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which setting would you like to edit?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));
        choices[action].Invoke();
    }

    private static void EditAppDataPath(Config.Server? server = null)
    {
        AnsiConsole.Clear();
        var sharedOptions = server is null ? Config.GlobalOptions : server.Options;
        AnsiConsole.MarkupLine(
            $"[bold]Current App Data Path:[/] [green]{(string.IsNullOrWhiteSpace(ConfigHelper.GetAppDataPath(server, server is null)) ? "Default" : ConfigHelper.GetAppDataPath(server))}[/]");
        sharedOptions.AppDataPath = AnsiConsole.Prompt(
            new TextPrompt<string?>("What is the new app data path?")
                .AllowEmpty());
        Config.Save();
    }

    private static void EditLaunchArgs(Config.Server? server = null)
    {
        AnsiConsole.Clear();
        // Display the current launch args in a prompt and allow the user to select which one to edit, or add a new one

        var choices = new Dictionary<string, Action>();
        foreach (var (key, value) in ConfigHelper.GetLaunchArgs(server, server is null))
        {
            choices.Add($"[bold]{key}[/] [green]{value}[/]", () => EditLaunchArg(key, server));
        }

        choices.Add("[bold]Add New Launch Argument[/]", () => AddLaunchArg(server));

        choices.Add("[bold]Back[/]", () => { });

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Which launch argument would you like to edit? (Server: {server?.Name ?? "Global"})")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));

        choices[action].Invoke();
    }

    private static void AddLaunchArg(Config.Server? server = null)
    {
        AnsiConsole.Clear();
        var sharedOptions = server is null ? Config.GlobalOptions : server.Options;
        var key = AnsiConsole.Prompt(
            new TextPrompt<string>("What is the launch argument key?"));

        var value = AnsiConsole.Prompt(
            new TextPrompt<string?>("What is the launch argument value?").AllowEmpty());

        sharedOptions.LaunchArgs.Add(key, value);

        Config.Save();
    }

    private static void EditLaunchArg(string key, Config.Server? server = null)
    {
        var leave = false;
        // Display the current key and value
        var sharedOptions = server is null ? Config.GlobalOptions : server.Options;
        while (!leave)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]Current Key:[/] [green]{key}[/]");
            AnsiConsole.MarkupLine($"[bold]Current Value:[/] [green]{sharedOptions.LaunchArgs[key]}[/]");
            AnsiConsole.WriteLine();

            var choices = new Dictionary<string, Action>
            {
                {
                    "Edit Value", () =>
                    {
                        sharedOptions.LaunchArgs[key] = AnsiConsole.Prompt(
                            new TextPrompt<string>("What is the new value?")
                                .AllowEmpty());
                        Config.Save();
                    }
                },
                {
                    "Edit Key", () =>
                    {
                        var newKey = AnsiConsole.Prompt(
                            new TextPrompt<string>("What is the new key?"));
                        var value = sharedOptions.LaunchArgs[key];
                        sharedOptions.LaunchArgs.Remove(key);
                        sharedOptions.LaunchArgs.Add(newKey, value);
                        key = newKey;
                        Config.Save();
                    }
                },
                { "Delete", () =>
                {
                    leave = true;
                    sharedOptions.LaunchArgs.Remove(key);
                    Config.Save();
                } },
                { "Back", () => leave = true }
            };

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"What would you like to do with this launch argument?")
                    .PageSize(10)
                    .AddChoices(choices.Keys.ToArray()));

            choices[action].Invoke();
        }
    }

    private static void AddServerMenu()
    {
        AnsiConsole.Clear();
        var server = new Config.Server
        {
            Name = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the name of the server?")),
            Port = AnsiConsole.Prompt(
                new TextPrompt<ushort>("What is the port of the server?").DefaultValue<ushort>(7777)),
            IncludeInLaunchAll =
                AnsiConsole.Confirm("Would you like to include this server in the launch all command?"),
        };
        var launchArgs = new Dictionary<string, string?>();
        while (AnsiConsole.Confirm("Would you like to add a launch argument?", false))
        {
            // Get the launch arg key
            var key = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the launch argument key?"));
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>("What is the launch argument value?"));

            launchArgs.Add(key, value);
        }

        server.Options.LaunchArgs = launchArgs;

        if (AnsiConsole.Confirm("Does this server have a custom app data path?", false))
        {
            server.Options.AppDataPath = AnsiConsole.Prompt(
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

    private static void DisplayServerInfo(Config.Server server)
    {
        AnsiConsole.Write(new Table().AddColumns("[grey]Option[/]", "[grey]Value[/]")
            .AddRow("[bold]Name[/]", server.Name)
            .AddRow("[bold]Port[/]", server.Port.ToString())
            .AddRow("[bold]Include In Launch All[/]", server.IncludeInLaunchAll.ToString())
            .AddRow("[bold]App Data Path[/]", server.Options.AppDataPath ?? "Default")
            .AddRow("[bold]Launch Args[/]",
                server.Options.LaunchArgs.Count > 0
                    ? string.Join(" ", server.Options.LaunchArgs.Select(x => $"{x.Key} {x.Value}"))
                    : "None"));
    }


    private static void EditServerMenu()
    {
        AnsiConsole.Clear();
        var choices = new Dictionary<string, Action>();

        foreach (var server in Config.Servers)
        {
            choices.Add($"[bold]{server.Name}[/] - Port: [green]{server.Port}[/]", () => EditServer(server));
        }

        choices.Add("[bold]Add New Server[/]", AddServerMenu);

        choices.Add("[bold]Back[/]", () => { });

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which server would you like to edit?")
                .PageSize(10)
                .AddChoices(choices.Keys.ToArray()));

        choices[action].Invoke();
    }

    private static void EditServer(Config.Server server)
    {
        var leave = false;

        while (!leave)
        {
            AnsiConsole.Clear();
            DisplayServerInfo(server);

            var choices = new Dictionary<string, Action>
            {
                {
                    "Edit Name", () =>
                    {
                        server.Name = AnsiConsole.Prompt(
                            new TextPrompt<string>("What is the new name?"));
                        Config.Save();
                    }
                },
                {
                    "Edit Port", () =>
                    {
                        server.Port = AnsiConsole.Prompt(
                            new TextPrompt<ushort>("What is the new port?").DefaultValue<ushort>(7777));
                        Config.Save();
                    }
                },
                { "Toggle Include In Launch All", () =>
                {
                    server.IncludeInLaunchAll = !server.IncludeInLaunchAll;
                    Config.Save();
                } },
                { "Edit App Data Path", () => EditAppDataPath(server) },
                { "Edit Launch Args", () => EditLaunchArgs(server) },
                { "Delete", () =>
                {
                    leave = true;
                    Config.Servers = Config.Servers.Where(x => x != server).ToArray();
                    Config.Save();
                } },
                { "Back", () => leave = true }
            };

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .PageSize(10)
                    .AddChoices(choices.Keys.ToArray()));

            choices[action].Invoke();
        }
    }

    private static void SpecificServerMenu()
    {
        AnsiConsole.Clear();

        if (Config.Servers.Length == 0)
        {
            AnsiConsole.MarkupLine("[bold][red]There are no servers to start.[/][/]");
            WaitFor(5);
            return;
        }

        var choices = new Dictionary<string, Action>();
        foreach (var server in Config.Servers)
        {
            choices.Add($"[bold]{server.Name}[/] - Port: [green]{server.Port}[/]", () => StartServer(server));
        }

        var action = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Which server would you like to start?")
                .PageSize(10)
                .NotRequired()
                .AddChoices(choices.Keys.ToArray()));

        if (action.Count == 0)
            return;

        foreach (var act in action)
        {
            choices[act].Invoke();
        }

        _exit = true;
    }

    private static void WaitFor(int seconds)
    {
        AnsiConsole.Status().Start("Press any key to continue in 5 seconds...", ctx =>
        {
            for (var i = 0; i < seconds * 10; i++)
            {
                ctx.Status($"Press any key to continue in {5 - i / 10} seconds...");
                if (Console.KeyAvailable)
                    return;
                Thread.Sleep(100);
            }
        });
    }

    private static void StartAllServers()
    {
        AnsiConsole.Clear();
        var serverToStart = Config.Servers.Where(x => x.IncludeInLaunchAll).ToArray();
        if (serverToStart.Length == 0)
        {
            AnsiConsole.MarkupLine("[bold][red]No servers are set to be included in the launch all command.[/][/]");
            WaitFor(5);
            return;
        }

        foreach (var server in serverToStart)
        {
            StartServer(server);
        }
    }

    private static void StartServer(Config.Server server)
    {
        var args = new List<string>
        {
            server.Port.ToString(),
            ConfigHelper.GetLaunchArgsString(server)
        };


        var appDataPth = ConfigHelper.GetAppDataPath(server);
        if (!string.IsNullOrWhiteSpace(appDataPth))
        {
            args.Add($"-appdatapath");
            args.Add(appDataPth);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = LocalAdminExecutable,
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };

        AnsiConsole.MarkupLine($"[bold]Starting {server.Name} on port {server.Port}[/]");

        Thread.Sleep(500);

        process.Start();
    }
}