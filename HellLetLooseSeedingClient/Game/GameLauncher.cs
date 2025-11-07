using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Game;

[SupportedOSPlatform("windows")]
public class GameLauncher(ILogger<GameLauncher> logger, IOptions<LaunchOptions> options)
{
    public static bool IsGameRunning()
    {
        var candidates = Process.GetProcessesByName("HLL-WIN64-Shipping");
        return candidates.Length != 0;
    }

    public async Task RunAndConnect(string ip, ushort port)
    {
        await BootHellLetLooseViaSteam(ip, port);
        var process = await WaitForHellLetLoose();
        await RunHellLetLooseStartupSequence(process);
    }

    private Task BootHellLetLooseViaSteam(string ip, ushort port)
    {
        var steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamExe", null)?.ToString();

        var processStartInfo = new ProcessStartInfo
        {
            FileName = steamPath,
            Arguments = $"-applaunch 686810 -dev +connect {ip}:{port}",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = true,
        };

        var process = Process.Start(processStartInfo);

        logger.LogInformation("Launching game");

        return process?.WaitForExitAsync() ?? Task.CompletedTask;
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
