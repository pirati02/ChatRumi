using ChatRumi.Feed.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ChatRumi.Feed.Infrastructure;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure(IConfiguration configuration
        )
        {
            var connectionString = configuration.GetConnectionString("FeedContext");
            // var user = configuration.GetSection("Elastic:User").Value;
            // var password = configuration.GetSection("Elastic:Password").Value;

            services.AddSingleton<IElasticClient>(_ =>
            {
                var settings = new ConnectionSettings(new Uri(connectionString!))
                    .DefaultIndex(PostIndexes.Posts)
                    .EnableDebugMode();

                return new ElasticClient(settings);
            });
        }
    }
}