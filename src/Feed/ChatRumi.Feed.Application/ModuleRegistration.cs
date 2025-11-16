using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Feed.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddMediatR(config =>
                config.RegisterServicesFromAssemblies(Application.Assembly));
        }
    }
}