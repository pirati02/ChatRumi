using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Hubs;

namespace ChatRumi.Chat.AccountSync;

public static class Dependency
{
    public static void AddHostedService(this IServiceCollection services)
    {
        services.AddSingleton<AccountConnectionManager>();
        services.AddScoped<IChatHubContextProxy, ChatHubContextProxy>();
        services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions = DefaultJsonContentOptions.CreateJsonOptions());
    }
}
