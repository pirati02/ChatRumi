using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Hubs;

namespace ChatRumi.Chat.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation()
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder =>
                {
                    policyBuilder.WithOrigins(
                            "http://localhost:4200",
                            "http://gateway:7000",
                            "http://localhost:7000"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            services.AddSingleton<AccountConnectionManager>();
            services.AddSingleton<IChatHubContextProxy, ChatHubContextProxy>();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            }).AddJsonProtocol(o => o.PayloadSerializerOptions = DefaultJsonContentOptions.CreateJsonOptions());
            services.AddOpenApi();
        }
    }
}
