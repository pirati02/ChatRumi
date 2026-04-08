using Microsoft.Extensions.DependencyInjection;
using ChatRum.InterCommunication;
using ChatRumi.Feed.Application.Outbox;

namespace ChatRumi.Feed.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddMediator(cfg => cfg.Assemblies = [typeof(IRefMarker)]);
            services.AddSingleton<IDispatcher, KafkaProducer>();
            services.AddScoped<IOutboxWriter, FeedOutboxWriter>();
            services.AddOptions<OutboxRelayOptions>().BindConfiguration(OutboxRelayOptions.SectionName);
        }
    }
}