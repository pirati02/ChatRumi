using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Infrastructure;
using ChatRumi.Notification.Api.Hub;
using ChatRumi.Notification.Api.Notifications;
using ChatRumi.Notification.Application;

namespace ChatRumi.Notification.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation(IConfiguration configuration)
        {
            services.AddChatRumiResponseCompression();
            services.AddChatRumiCorsFromConfiguration(configuration, "CorsPolicy");
            services.AddChatRumiJwtAuthentication(configuration);
            services.AddConsulService(configuration);
            services.AddOpenTelemetryObservability(configuration);
            services.AddSignalR();
            services.AddSingleton<NotificationConnectionManager>();
            services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
        }
    }
}
