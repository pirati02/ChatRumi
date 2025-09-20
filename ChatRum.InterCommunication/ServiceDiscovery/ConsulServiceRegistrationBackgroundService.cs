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
        _serviceId = $"{serviceName}-{Guid.NewGuid()}";

        var uri = new Uri(options.Value.ServiceAddress);
 
        var services = await consulClient.Agent.Services(cancellationToken);
        if (services.Response.Values.Any(s => s.ID == _serviceId))
        {
            Console.WriteLine($"⚠️ Service {_serviceId} is already registered in Consul. Skipping registration.");
            return;
        }
        
        var registration = new AgentServiceRegistration
        {
            ID = _serviceId,
            Name = serviceName,
            Address = uri.Host,
            Port = uri.Port,
            Tags = ["api"],
            Check = new AgentServiceCheck
            {
                HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
            }
        };

        await consulClient.Agent.ServiceRegister(registration, cancellationToken);
        Console.WriteLine("Health Check URL: " + $"{uri.Scheme}://{uri.Host}:{uri.Port}/health");
        Console.WriteLine($"✅ Registered {serviceName} with Consul at {uri}");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_serviceId))
        {
            await consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
            _serviceId = null;
            Console.WriteLine($"❌ Deregistered {_serviceId} from Consul");
        }
    }
}