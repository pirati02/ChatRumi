using ChatRumi.Friendship.Application;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ChatRumi.Friendship.Api;

public static class Dependency
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddOptions<ApplicationOptions>()
            .BindConfiguration(ApplicationOptions.Name);
        services.AddSingleton<IDriver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ApplicationOptions>>().Value;
            return GraphDatabase.Driver(options.Neo4jConnection,  AuthTokens.Basic(options.Neo4jUser, options.Neo4jPassword));
        });

        return services;
    }
}