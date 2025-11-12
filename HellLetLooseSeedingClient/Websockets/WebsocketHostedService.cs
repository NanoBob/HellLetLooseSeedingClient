using HellLetLooseSeedingClient.Notifications;
using HellLetLooseSeedingClient.Tray;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HellLetLooseSeedingClient.Websockets;

public class WebsocketHostedService(
    IOptions<WebsocketOptions> options, 
    SeedingWebsocketClient client,
    AppNotificationService notificationService,
    SystemTrayService systemTrayService,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<WebsocketHostedService> logger) : IHostedService
{
    private bool isRunning = false;

    private Task? loopTask;
    private CancellationTokenSource? connectCancellationToken;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (isRunning)
            return;

        await systemTrayService.CreateSystemTrayIcon();

        systemTrayService.EnableRequested += HandleEnableRequest;
        systemTrayService.DisableRequested += HandleDisableRequest;
        systemTrayService.ExitRequested += HandleExitRequest;

        notificationService.ShowInformationalToast("Seeding client", "Seeding client has started.");

        isRunning = true;
        loopTask = ConnectLoop(cancellationToken);

        _ = HandleAutostart();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!isRunning)
            return;

        systemTrayService.EnableRequested -= HandleEnableRequest;
        systemTrayService.DisableRequested -= HandleDisableRequest;
        systemTrayService.ExitRequested -= HandleExitRequest;

        isRunning = false;

        connectCancellationToken?.Cancel();
        connectCancellationToken?.Dispose();
        await (loopTask ?? Task.CompletedTask);

        systemTrayService.DestroySystemTrayIcon();
    }

    private async Task HandleAutostart()
    {
        systemTrayService.SetAutostartEnabled(StartupProcessHelper.IsAutostartSetUp());

        if (!StartupProcessHelper.IsAutostartSetUp())
        {
            if (await notificationService.RequestApprovalAsync("Setup-autostart?", "Do you want to setup auto-start so this application start automatically when your PC boots?", "Yes", "No") == ApprovalResult.Approved)
            {
                await StartupProcessHelper.RequestSetupAutostartAsync();
                systemTrayService.SetAutostartEnabled(StartupProcessHelper.IsAutostartSetUp());
            }
        }
    }

    private async void HandleEnableRequest(object? sender, EventArgs e)
    {
        await StartupProcessHelper.RequestSetupAutostartAsync();

        systemTrayService.SetAutostartEnabled(StartupProcessHelper.IsAutostartSetUp());
    }

    private async void HandleDisableRequest(object? sender, EventArgs e)
    {
        await StartupProcessHelper.RequestRemoveAutostartAsync();

        systemTrayService.SetAutostartEnabled(StartupProcessHelper.IsAutostartSetUp());
    }

    private void HandleExitRequest(object? sender, EventArgs e)
    {
        hostApplicationLifetime.StopApplication();
    }

    private async Task ConnectLoop(CancellationToken cancellationToken)
    {
        while (isRunning)
        {
            try
            {
                connectCancellationToken?.Dispose();
                connectCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                await TryConnectAsync(connectCancellationToken.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Websocket connection to {url} failed: {Message}", options.Value.Url, ex.Message);
            }
            if (isRunning)
            {
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    private async Task TryConnectAsync(CancellationToken cancellationToken)
    {
        await client.ConnectAsync(options.Value.Url, cancellationToken);
    }
}
