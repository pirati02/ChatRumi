using ChatRumi.Friendship.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ChatRumi.Friendship.Application;

public static class Dependency
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddOptions<Neo4jOptions>().BindConfiguration(Neo4jOptions.Name);
        services.AddSingleton<IDriver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            return GraphDatabase.Driver(options.Neo4jConnection,
                AuthTokens.Basic(options.Neo4jUser, options.Neo4jPassword));
        });

        services.AddScoped<IPeerConnectionManager, PeerConnectionManager>();
    }
}