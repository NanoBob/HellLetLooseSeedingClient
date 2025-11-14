using HellLetLooseSeedingClient.Notifications;
using IniParser;
using IniParser.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace HellLetLooseSeedingClient.Game;

[SupportedOSPlatform("windows")]
public class GameLauncher(ILogger<GameLauncher> logger, IOptionsMonitor<LaunchOptions> options)
{
    private const string hellLetLooseAppId = "686810";

    public static bool IsGameRunning()
    {
        var candidates = Process.GetProcessesByName("HLL-WIN64-Shipping");
        return candidates.Length != 0;
    }

    public async Task<bool> RunAndConnect(string ip, ushort port)
    {
        var copy = AdjustGameUserSettings();
        try
        {
            if (!await BootHellLetLooseViaSteam(ip, port))
                return false;

            var process = await WaitForHellLetLoose();
            await RunHellLetLooseStartupSequence(process);

            return true;
        } finally
        {
            if (copy != null)
                RestoreGameUserSettings(copy);
        }
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

    private string? AdjustGameUserSettings() 
    {
        if (!options.CurrentValue.SaveSystemResources)
            return null;

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HLL",
            "Saved",
            "Config",
            "WindowsNoEditor",
            "GameUserSettings.ini");   
        
        var copyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HLL",
            "Saved",
            "Config",
            "WindowsNoEditor",
            $"OriginalGameUserSettings-{DateTime.UtcNow.Ticks}.ini");

        File.Copy(path, copyPath, true);

        var parser = new FileIniDataParser(new IniParser.Parser.IniDataParser(new IniParser.Model.Configuration.IniParserConfiguration()
        {
            AllowDuplicateKeys = true,
            ConcatenateDuplicateKeys = true,
            SkipInvalidLines = true
        }));
        IniData data = parser.ReadFile(path);
        AdjustIniFile(data);
        parser.WriteFile(path, data);

        return copyPath;
    }

    private static void AdjustIniFile(IniData data)
    {
        data["/Script/HLL.ShooterGameUserSettings"]["FullscreenMode"] = "2";
        data["/Script/HLL.ShooterGameUserSettings"]["LastConfirmedFullscreenMode"] = "2";
        data["/Script/HLL.ShooterGameUserSettings"]["PreferredFullscreenMode"] = "2";

        data["/Script/HLL.ShooterGameUserSettings"]["ResolutionSizeX"] = "1024";
        data["/Script/HLL.ShooterGameUserSettings"]["LastUserConfirmedResolutionSizeX"] = "1024";

        data["/Script/HLL.ShooterGameUserSettings"]["ResolutionSizeY"] = "768";
        data["/Script/HLL.ShooterGameUserSettings"]["LastUserConfirmedResolutionSizeY"] = "768";

        data["/Script/HLL.ShooterGameUserSettings"]["FrameRateLimit"] = "30";        

        data["ScalabilityGroups"]["sg.ResolutionQuality"] = "35.0";
        data["ScalabilityGroups"]["sg.ViewDistanceQuality"] = "1";
        data["ScalabilityGroups"]["sg.AntiAliasingQuality"] = "1";
        data["ScalabilityGroups"]["sg.ShadowQuality"] = "1";
        data["ScalabilityGroups"]["sg.PostProcessQuality"] = "1";
        data["ScalabilityGroups"]["sg.TextureQuality"] = "1";
        data["ScalabilityGroups"]["sg.EffectsQuality"] = "1";
        data["ScalabilityGroups"]["sg.FoliageQuality"] = "1";
        data["ScalabilityGroups"]["sg.ShadingQuality"] = "1";
    }

    private void RestoreGameUserSettings(string backupPath)
    {
        if (!File.Exists(backupPath))
            return;

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HLL",
            "Saved",
            "Config",
            "WindowsNoEditor",
            "GameUserSettings.ini");

        File.Copy(backupPath, path, true);
        File.Delete(backupPath);
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
        await Task.Delay(options.CurrentValue.FirstClickDelay);
        logger.LogInformation("Bringing to front + first enter");

        NativeHelper.BringToFront(process);
        NativeHelper.SendKeyPress(Keys.Enter);

        await Task.Delay(options.CurrentValue.SecondClickDelay);
        logger.LogInformation("Bringing to front + second enter");
        NativeHelper.BringToFront(process);
        NativeHelper.SendKeyPress(Keys.Enter);
    }
}
