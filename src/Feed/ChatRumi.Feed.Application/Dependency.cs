using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Feed.Application;

public static class Dependency
{
    public static void AddApplication(
        this IServiceCollection services
    )
    {
        services.AddMediatR(config =>
            config.RegisterServicesFromAssemblies(Application.Assembly));
    }
}