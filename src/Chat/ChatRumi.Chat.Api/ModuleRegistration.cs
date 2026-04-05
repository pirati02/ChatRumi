using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace ChatRumi.Chat.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation(IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddChatRumiResponseCompression();
            services.AddChatRumiCorsFromConfiguration(configuration, "CorsPolicy");

            services.AddSingleton<AccountConnectionManager>();
            services.AddSingleton<IChatHubContextProxy, ChatHubContextProxy>();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = environment.IsDevelopment();
            }).AddJsonProtocol(o => o.PayloadSerializerOptions = DefaultJsonContentOptions.CreateJsonOptions());
            if (environment.IsDevelopment())
            {
                services.AddOpenApi();
            }

            services.AddChatRumiJwtAuthentication(configuration);
            services.AddConsulService(configuration);
            services.AddOpenTelemetryObservability(configuration);
        }
    }
}
