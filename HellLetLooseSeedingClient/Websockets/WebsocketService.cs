using HellLetLooseSeedingClient.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HellLetLooseSeedingClient.Websockets;

public class WebsocketService(IOptions<WebsocketOptions> options, SeedingWebsocketClient client, ILogger<WebsocketService> logger, AppNotificationService notificationService) : IHostedService
{
    private bool isRunning = false;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (isRunning)
            return;

        notificationService.ShowInformationalToast("Seeding client", "Seeding client has started.");

        isRunning = true;
        _ = ConnectLoop(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!isRunning)
            return;

        isRunning = false;
    }

    private async Task ConnectLoop(CancellationToken cancellationToken)
    {
        while (isRunning)
        {
            try
            {
                await TryConnectAsync(cancellationToken);
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
