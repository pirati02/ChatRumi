using Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ChatRum.InterCommunication.ServiceDiscovery;

public static class ConsulDependency
{
    public static void AddConsulService(this IServiceCollection services)
    {
        services.AddOptions<ConsulOptions>()
            .BindConfiguration(ConsulOptions.Name)
            .ValidateOnStart();
        services.AddSingleton<IConsulClient, ConsulClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ConsulOptions>>().Value;
            var consulUri = new Uri($"http://{opts.Host}:{opts.Port}");
            return new ConsulClient(c => { c.Address = consulUri; });
        });
        services.AddSingleton<IHostedService, ConsulServiceRegistrationBackgroundService>();
    }
}