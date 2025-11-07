namespace HellLetLooseSeedingClient.Websockets;

public record BaseRemoteSeederCommand(string Type);

public record RequestSeedCommand(string Type, string Ip, ushort Port) : BaseRemoteSeederCommand(Type);

public record ReadyCommand(string Type, DateTime StartedAtUtc) : BaseRemoteSeederCommand(Type);
public record RejectSeedCommand(string Type, DateTime RejectedAtUtc, DateTime RejectedUntilUtc) : BaseRemoteSeederCommand(Type);
public record BootingCommand(string Type, DateTime StartedAtUtc) : BaseRemoteSeederCommand(Type);
public record RunningCommand(string Type, DateTime StartedAtUtc) : BaseRemoteSeederCommand(Type);

