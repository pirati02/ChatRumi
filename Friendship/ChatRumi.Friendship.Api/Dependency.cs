using ChatRum.InterCommunication;
using ChatRumi.Friendship.Application;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ChatRumi.Friendship.Api;

public static class Dependency
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:4200") // Angular frontend URL
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Important for SignalR
            });
        });
        services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
        services.AddOptions<Neo4jOptions>().BindConfiguration(Neo4jOptions.Name);
        services.AddSingleton<IDriver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            return GraphDatabase.Driver(options.Neo4jConnection,  AuthTokens.Basic(options.Neo4jUser, options.Neo4jPassword));
        });

        services.AddHostedService<AccountCreatedConsumerBackgroundService>();
        
        return services;
    }
}