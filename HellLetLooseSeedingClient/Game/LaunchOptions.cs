namespace HellLetLooseSeedingClient.Game;

public class LaunchOptions
{
    public TimeSpan FirstClickDelay { get; init; } = TimeSpan.FromSeconds(12.5);
    public TimeSpan SecondClickDelay { get; init; } = TimeSpan.FromSeconds(12.5);
}
