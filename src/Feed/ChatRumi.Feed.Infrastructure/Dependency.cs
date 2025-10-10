using ChatRumi.Feed.Application;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Feed.Infrastructure;

public static class Dependency
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // services.AddOptions("")
        var connectionString = configuration.GetConnectionString("FeedContext");
        var user = configuration.GetSection("Elastic:User").Value;
        var password = configuration.GetSection("Elastic:Password").Value;

        services.AddSingleton(sp =>
        {
            var settings = new ElasticsearchClientSettings(new Uri(connectionString!))
                .Authentication(new BasicAuthentication(user!, password!))
                .DefaultIndex(PostIndexes.Posts);

            return new ElasticsearchClient(settings);
        });
    }
}