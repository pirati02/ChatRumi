using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Application.Assembly));
        }
    }
}