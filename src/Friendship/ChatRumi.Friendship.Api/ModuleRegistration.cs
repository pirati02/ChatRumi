using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Friendship.Api.Hub;
using ChatRumi.Friendship.Application.Services;
using ChatRumi.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace ChatRumi.Friendship.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation(IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddChatRumiResponseCompression();
            if (environment.IsDevelopment())
            {
                services.AddOpenApi();
            }

            services.AddChatRumiCorsFromConfiguration(configuration, "CorsPolicy");

            services.AddSignalR();
            services.AddSingleton<FriendshipConnectionManager>();
            services.AddScoped<IFriendshipHubContextProxy, FriendshipHubContextProxy>();

            services.AddChatRumiJwtAuthentication(configuration);
            services.AddConsulService(configuration);
            services.AddOpenTelemetryObservability(configuration);
        }
    }
}