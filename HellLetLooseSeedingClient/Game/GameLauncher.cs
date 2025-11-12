using HellLetLooseSeedingClient.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Game;

[SupportedOSPlatform("windows")]
public class GameLauncher(ILogger<GameLauncher> logger, IOptions<LaunchOptions> options)
{
    private const string hellLetLooseAppId = "686810";

    public static bool IsGameRunning()
    {
        var candidates = Process.GetProcessesByName("HLL-WIN64-Shipping");
        return candidates.Length != 0;
    }

    public async Task<bool> RunAndConnect(string ip, ushort port)
    {
        if (!await BootHellLetLooseViaSteam(ip, port))
            return false;

        var process = await WaitForHellLetLoose();
        await RunHellLetLooseStartupSequence(process);

        return true;
    }

    private async Task<bool> BootHellLetLooseViaSteam(string ip, ushort port)
    {
        var steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamExe", null)?.ToString();

        var processStartInfo = new ProcessStartInfo
        {
            FileName = steamPath,
            Arguments = $"-applaunch {hellLetLooseAppId} -dev +connect {ip}:{port}",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = true,
        };

        try
        {
            var process = Process.Start(processStartInfo);

            logger.LogInformation("Launching game");

            await (process?.WaitForExitAsync() ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to launch Hell Let Loose via Steam");
            AppNotificationService.ShowErrorToast("Launch failed", $"Failed to launch Hell Let Loose via Steam: {ex.Message}");
            return false;
        }
    }

    private async Task<Process> WaitForHellLetLoose()
    {
        IEnumerable<Process> candidates;
        do
        {
            await Task.Delay(1000);
            candidates = Process.GetProcessesByName("HLL-WIN64-Shipping");
        } while (!candidates.Any());

        logger.LogInformation("Game launched");

        return candidates.Single();
    }

    private async Task RunHellLetLooseStartupSequence(Process process)
    {
        await Task.Delay(options.Value.FirstClickDelay);
        logger.LogInformation("Bringing to front + first enter");

        NativeHelper.BringToFront(process);
        NativeHelper.SendKeyPress(Keys.Enter);

        await Task.Delay(options.Value.SecondClickDelay);
        logger.LogInformation("Bringing to front + second enter");
        NativeHelper.BringToFront(process);
        NativeHelper.SendKeyPress(Keys.Enter);
    }
}
