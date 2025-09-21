namespace ChatRum.InterCommunication.ServiceDiscovery;

public class ConsulOptions
{
    public const string Name = nameof(ConsulOptions);
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public required string ServiceName { get; init; }
    public required string Scheme { get; init; }
    public required string ServiceAddress { get; init; }
    public bool Enabled { get; init; }
}