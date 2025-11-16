using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Hubs;

namespace ChatRumi.Chat.AccountSync;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddHostedService()
        {
            services.AddSingleton<AccountConnectionManager>();
            services.AddScoped<IChatHubContextProxy, ChatHubContextProxy>();
            services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions = DefaultJsonContentOptions.CreateJsonOptions());
        }
    }
}
