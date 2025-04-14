using Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChatRum.InterCommunication.ServiceDiscovery;

public static class ConsulDependency
{
    public static void AddConsulService(this IServiceCollection services)
    {
        services.AddOptions<ConsulOptions>()
            .BindConfiguration(ConsulOptions.Name)
            .ValidateOnStart();
        services.AddSingleton<IConsulClient, ConsulClient>();
        services.AddSingleton<IHostedService, ConsulServiceRegistration>();
    }
}