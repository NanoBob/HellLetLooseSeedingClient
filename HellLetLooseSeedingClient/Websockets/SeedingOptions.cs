namespace HellLetLooseSeedingClient.Websockets;

public class SeedingOptions
{
    public TimeSpan RejectionDuration { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan NotificationDuration { get; init; } = TimeSpan.FromMinutes(60);
    public bool RejectByAnyInput { get; init; } = true;
}
