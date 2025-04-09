using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Friendship.Application;

public static class Dependency
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPeerConnectionManager, PeerConnectionManager>();
    }
}