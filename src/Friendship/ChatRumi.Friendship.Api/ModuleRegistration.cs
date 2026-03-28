using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Friendship.Api.Hub;
using ChatRumi.Friendship.Application.Services;
using ChatRumi.Infrastructure;

namespace ChatRumi.Friendship.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation(IConfiguration configuration)
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

            services.AddSignalR();
            services.AddSingleton<FriendshipConnectionManager>();
            services.AddScoped<IFriendshipHubContextProxy, FriendshipHubContextProxy>();

            services.AddChatRumiJwtAuthentication(configuration);
            services.AddConsulService(configuration);
            services.AddOpenTelemetryObservability(configuration);
        }
    }
}