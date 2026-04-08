using ChatRumi.Notification.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ChatRumi.Notification.Infrastructure;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("FeedContext");

            services.AddSingleton<IElasticClient>(_ =>
            {
                var settings = new ConnectionSettings(new Uri(connectionString!))
                    .DefaultIndex(NotificationIndexes.Notifications)
                    .EnableDebugMode();

                return new ElasticClient(settings);
            });
        }
    }
}
