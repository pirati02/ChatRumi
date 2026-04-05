using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Infrastructure;

/// <summary>
/// Binds CORS from <c>Cors:AllowedOrigins</c> and optional <c>Cors:AllowCredentials</c>.
/// </summary>
public static class ChatRumiCorsExtensions
{
    private static readonly string[] DefaultOrigins =
    [
        "http://localhost:4200",
        "http://gateway:7000",
        "http://localhost:7000"
    ];

    public static IServiceCollection AddChatRumiCorsFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
        {
            origins = DefaultOrigins;
        }

        var allowCredentials = configuration.GetValue("Cors:AllowCredentials", true);

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.WithOrigins(origins);
                if (allowCredentials)
                {
                    policy.AllowCredentials();
                }

                policy.AllowAnyHeader();
                policy.AllowAnyMethod();
            });
        });

        return services;
    }
}
