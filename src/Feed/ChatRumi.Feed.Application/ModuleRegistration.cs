using Microsoft.Extensions.DependencyInjection;
using ChatRum.InterCommunication;

namespace ChatRumi.Feed.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddMediator(cfg => cfg.Assemblies = [typeof(IRefMarker)]);
            services.AddSingleton<IDispatcher, KafkaProducer>();
        }
    }
}