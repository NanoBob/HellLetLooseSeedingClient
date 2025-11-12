using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Security.Principal;

namespace HellLetLooseSeedingClient;

public static class StartupProcessHelper
{
    public const string RegisterTaskArgument = "register-scheduled-task";
    public const string RemoveTaskArgument = "remove-scheduled-task";

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

    /// <summary>
    /// This method creates a task in the Windows task scheduler
    /// This method will spawn a new admin process to do this
    /// </summary>
    /// <returns></returns>
    public static async System.Threading.Tasks.Task RequestSetupAutostartAsync()
    {
        await LaunchAsAdmin(RegisterTaskArgument);
    }

    /// <summary>
    /// This method removes the created task in the Windows task scheduler
    /// This method will spawn a new admin process to do this
    /// </summary>
    /// <returns></returns>
    public static async System.Threading.Tasks.Task RequestRemoveAutostartAsync()
    {
        await LaunchAsAdmin(RemoveTaskArgument);
    }

    /// <summary>
    /// This method creates a task in the Windows task scheduler
    /// This method throws when there is insufficient permissions
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// This method removes the created task in the Windows task scheduler
    /// This method throws when there is insufficient permissions
    /// </summary>
    /// <returns></returns>
    public static void RemoveAutostart()
    {
        using var taskService = new TaskService();
        taskService.RootFolder.DeleteTask(TaskName, false);
    }

    public static async Task<bool> LaunchAsAdmin(string arguments = "")
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(exePath))
                throw new LaunchAsAdminFailedException("Unable to start as admin, executable path could not be determined.");

            var args = Environment.GetCommandLineArgs().Skip(1);
            var argString = $"{arguments} {string.Join(' ', args.Select(QuoteArgument))}";

            var psi = new ProcessStartInfo(exePath, argString)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath),
            };

            var process = Process.Start(psi);
            await process!.WaitForExitAsync();

            return true;
        }
        catch (Exception e)
        {
            throw new LaunchAsAdminFailedException("Unable to start as admin.", e);
        }
    }

    public static async Task<bool> RelaunchAsAdminIfNeeded(bool exitAfterComplete = true)
    {
        if (IsRunningAsAdministrator())
            return false;

        var result = await LaunchAsAdmin();

        if (exitAfterComplete)
            Environment.Exit(0);

        return result;
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

public class LaunchAsAdminFailedException(string message, Exception? innerException = null) : Exception(message, innerException)
{
}