using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            services.Configure<JsonOptions>(o =>
            {
                o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.SerializerOptions.WriteIndented = false;
                o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                o.SerializerOptions.Converters.Add(new MessageContentConverter());
            });
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
