using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;

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

    public static void SetupAutostart()
    {
        var exePath = Environment.ProcessPath;

        using var taskService = new TaskService();

        var definition = taskService.NewTask();
        definition.RegistrationInfo.Description = Description;

        definition.Triggers.Add(new LogonTrigger());
        definition.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

        definition.Principal.RunLevel = TaskRunLevel.LUA;
        taskService.RootFolder.RegisterTaskDefinition(TaskName, definition);
    }
}