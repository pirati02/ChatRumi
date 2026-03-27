using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddMediator(cfg => cfg.Assemblies = [Application.Assembly]);
        }
    }
}