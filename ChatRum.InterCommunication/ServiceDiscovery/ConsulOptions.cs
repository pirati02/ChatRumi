namespace ChatRum.InterCommunication.ServiceDiscovery;

public class ConsulOptions
{
    public const string Name = nameof(ConsulOptions);
    public required string ServiceName { get; set; }
    public required string ServiceAddress { get; set; }
}