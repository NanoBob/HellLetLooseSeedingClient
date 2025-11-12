using HellLetLooseSeedingClient.Game;
using HellLetLooseSeedingClient.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace HellLetLooseSeedingClient.Websockets;

[SupportedOSPlatform("windows")]
public class SeedingWebsocketClient(
    GameLauncher launcher, 
    ILogger<SeedingWebsocketClient> logger, 
    AppNotificationService notifications, 
    IOptions<SeedingOptions> options)
{
    private ClientWebSocket socket = new();
    private CancellationToken cancellationToken = CancellationToken.None;

    private SeedingState state = SeedingState.Ready;

    private DateTime lastBootUtc;
    private DateTime rejectUntilUtc;

    public async Task ConnectAsync(string url, CancellationToken? cancellationToken = null)
    {
        this.cancellationToken = cancellationToken ?? CancellationToken.None;

        this.socket = new();
        await this.socket.ConnectAsync(new Uri(url), this.cancellationToken);

        notifications.ShowInformationalToast("Seeding client", "Seeding client has connected.");

        if (this.state != SeedingState.Rejected)
        {
            if (GameLauncher.IsGameRunning())
                await SetRunning();
            else
                await SetReady();
        }

        _ = Task.Run(RelayGameStateAsync);
        await ReceiveLoop();
    }

    public Task SetReady()
    {
        this.state = SeedingState.Ready;
        return Send(new ReadyCommand(nameof(ReadyCommand), DateTime.UtcNow));
    }

    public Task SetBooting()
    {
        this.state = SeedingState.Booting;
        this.lastBootUtc = DateTime.UtcNow;
        return Send(new BootingCommand(nameof(BootingCommand), DateTime.UtcNow));
    }

    public Task SetRunning()
    {
        this.state = SeedingState.Booting;
        return Send(new RunningCommand(nameof(RunningCommand), DateTime.UtcNow));
    }

    public Task SetRejected(TimeSpan rejectTime)
    {
        this.rejectUntilUtc = DateTime.UtcNow + rejectTime;
        this.state = SeedingState.Rejected;

        return Send(new RejectSeedCommand(
            nameof(RejectSeedCommand),
            DateTime.UtcNow,
            this.rejectUntilUtc));
    }

    private Task Send(BaseRemoteSeederCommand command)
    {
        logger.LogInformation("Sending a {command}", command.Type);

        if (this.socket is null || this.socket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected.");
        }

        var message = JsonSerializer.Serialize(command);
        var messageBuffer = Encoding.UTF8.GetBytes(message);

        var segment = new ArraySegment<byte>(messageBuffer);
        return this.socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task RelayGameStateAsync()
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var isGameRunning = GameLauncher.IsGameRunning();

            if (!isGameRunning && state == SeedingState.Running)
                await SetReady();

            else if (!isGameRunning && state == SeedingState.Booting && lastBootUtc < DateTime.UtcNow - TimeSpan.FromMinutes(10))
                await SetReady();

            else if (isGameRunning && state == SeedingState.Ready)
                await SetRunning();

            else if (state == SeedingState.Rejected && rejectUntilUtc < DateTime.UtcNow)
            {
                if (isGameRunning)
                    await SetRunning();
                else
                    await SetReady();
            }

            await Task.Delay(10000, cancellationToken);
        }
    }

    private async Task ReceiveLoop()
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await ReceiveMessageAsync(cancellationToken);
            if (message is null)
                break;

            try
            {
                using var doc = JsonDocument.Parse(message);
                if (!doc.RootElement.TryGetProperty("Type", out var typeProp))
                    continue;

                var type = typeProp.GetString();
                if (string.Equals(type, nameof(RequestSeedCommand), StringComparison.Ordinal))
                {
                    var request = JsonSerializer.Deserialize<RequestSeedCommand>(message);
                    if (request is not null)
                        await HandleRequestSeedCommand(request);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to process message: {Message}", ex.Message);
#if DEBUG
                Debugger.BreakForUserUnhandledException(ex);
#endif
            }
        }
    }

    private async Task HandleRequestSeedCommand(RequestSeedCommand command)
    {
        if (state != SeedingState.Ready)
            return;

        if (!IPAddress.TryParse(command.Ip, out var _))
        {
            logger.LogWarning("Invalid IP address in RequestSeedCommand: {Ip}, rejecting.", command.Ip);
            await SetRejected(options.Value.RejectionDuration);
            return;
        }

        logger.LogInformation("RequestSeedCommand received: {Ip}:{Port}", command.Ip, command.Port);

        var approved = await notifications.RequestApprovalAsync(
            "Seed request",
            "Draft is requesting you to help seed. Will you join?",
            options.Value.NotificationDuration,
            cancellationToken);

        if (!approved)
        {
            logger.LogInformation("User rejected seed request via toast.");
            await SetRejected(options.Value.RejectionDuration);
            return;
        }

        await SetBooting();
        await launcher.RunAndConnect(command.Ip, command.Port);
        await SetRunning();
    }

    private async Task<string?> ReceiveMessageAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                return null;
            }

            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }

    public enum SeedingState
    {
        Ready,
        Booting,
        Running,
        Rejected
    }
}
