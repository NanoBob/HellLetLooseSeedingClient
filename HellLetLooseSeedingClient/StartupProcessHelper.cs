using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Security.Principal;

namespace HellLetLooseSeedingClient;

public static class StartupProcessHelper
{
    public const string TaskName = "HellLetLooseSeedingClient";
    public const string Description = """
        Boots Hell Let Loose when seeding is requested.
        """;

    public static bool IsAutostartSetUp()
    {
        var exePath = Environment.ProcessPath;
        using var process = Process.GetCurrentProcess();

        using var taskService = new TaskService();
        return taskService.RootFolder.Tasks.Any(t => t.Name == TaskName && t.Definition.Actions.OfType<ExecAction>().Any(a => a.Path == exePath));
    }

    public static async System.Threading.Tasks.Task SetupAutostartAsync()
    {
        await RelaunchAsAdminIfNeeded();

        var exePath = Environment.ProcessPath;

        using var taskService = new TaskService();

        var definition = taskService.NewTask();
        definition.RegistrationInfo.Description = Description;

        definition.Triggers.Add(new LogonTrigger());
        definition.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

        definition.Principal.RunLevel = TaskRunLevel.LUA;
        taskService.RootFolder.RegisterTaskDefinition(TaskName, definition);
    }

    public static async System.Threading.Tasks.Task RelaunchAsAdminIfNeeded()
    {
        if (IsRunningAsAdministrator())
            return;

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(exePath))
                return;

            var args = Environment.GetCommandLineArgs().Skip(1);
            var argString = string.Join(' ', args.Select(QuoteArgument));

            var psi = new ProcessStartInfo(exePath, argString)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath)
            };

            var process = Process.Start(psi);
            await process!.WaitForExitAsync();

            Environment.Exit(0);
        }
        catch
        {
            return;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string QuoteArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (arg.Contains(' ') || arg.Contains('\t') || arg.Contains('"'))
            return '"' + arg.Replace("\"", "\\\"") + '"';

        return arg;
    }
}