namespace ChatRum.InterCommunication.ServiceDiscovery;

public class ConsulOptions
{
    public const string Name = nameof(ConsulOptions);
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public required string ServiceName { get; set; }
    public required string ServiceAddress { get; set; }
}