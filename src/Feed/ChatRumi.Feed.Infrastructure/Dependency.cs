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
        services.AddSingleton(sp =>
        {
            var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
                .Authentication(new BasicAuthentication("elastic", "password"))
                .DefaultIndex("logs");

            return new ElasticsearchClient(settings);
        });
    }
}