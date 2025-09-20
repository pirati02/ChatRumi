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
        services.AddSingleton<IConsulClient, ConsulClient>(_ => new ConsulClient
        {
            Config =
            {
                Address = new Uri("http://localhost:8500")
            }
        });
        services.AddSingleton<IHostedService, ConsulServiceRegistrationBackgroundService>();
    }
}