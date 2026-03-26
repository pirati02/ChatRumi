using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ChatRum.InterCommunication.ServiceDiscovery;

public class ConsulServiceRegistrationBackgroundService(
    IConsulClient consulClient,
    IOptions<ConsulOptions> options
)
    : IHostedService
{
    private string? _serviceId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceName = options.Value.ServiceName;
        var uri = new Uri(options.Value.ServiceAddress);
        _serviceId = $"{serviceName}-{uri.Port}";

        // Always deregister first to ensure clean state
        try
        {
            await consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
            Console.WriteLine($"🔄 Deregistered existing {_serviceId} (if any) to ensure clean registration.");
        }
        catch
        {
            // Ignore if service doesn't exist
        }

        // Extract the Docker service name from the URI host
        // This must match the Docker Compose service name exactly (e.g., "account-service")
        var dockerServiceName = uri.Host;

        var registration = new AgentServiceRegistration
        {
            ID = _serviceId,
            Name = serviceName,
            Address = dockerServiceName,   // Docker service name for DNS resolution
            Port = uri.Port,
            Meta = new Dictionary<string, string>
            {
                ["ServiceUrl"] = options.Value.ServiceAddress,
                ["DockerServiceName"] = dockerServiceName
            },
            Tags = ["api", "docker"],
            Check = new AgentServiceCheck
            {
                HTTP = $"{uri.Scheme}://{dockerServiceName}:{uri.Port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(60)
            }
        };

        await consulClient.Agent.ServiceRegister(registration, cancellationToken);
        Console.WriteLine($"✅ Registered {serviceName} with Consul:");
        Console.WriteLine($"   Service ID: {_serviceId}");
        Console.WriteLine($"   Address: {dockerServiceName}:{uri.Port}");
        Console.WriteLine($"   Health Check: {uri.Scheme}://{dockerServiceName}:{uri.Port}/health");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_serviceId))
        {
            await consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
            Console.WriteLine($"❌ Deregistered {_serviceId} from Consul");
            _serviceId = null;
        }
    }
}