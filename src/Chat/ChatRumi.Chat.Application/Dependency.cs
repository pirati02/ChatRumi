using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application;

public static class Dependency
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Application.Assembly));
    }
}