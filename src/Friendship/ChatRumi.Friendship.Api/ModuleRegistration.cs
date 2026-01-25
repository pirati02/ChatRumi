using ChatRumi.Friendship.Api.Hub;
using ChatRumi.Friendship.Application.Services;
 
namespace ChatRumi.Friendship.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPresentation()
        {
            services.AddOpenApi();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder =>
                {
                    policyBuilder.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            
            services.AddSignalR();
            services.AddSingleton<FriendshipConnectionManager>();
            services.AddScoped<IFriendshipHubContextProxy, FriendshipHubContextProxy>();
            return services;
        }
    }
}