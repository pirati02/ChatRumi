using ChatRum.InterCommunication;

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
                policyBuilder.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        
        return services;
    }
}