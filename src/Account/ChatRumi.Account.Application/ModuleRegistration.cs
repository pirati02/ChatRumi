using ChatRum.InterCommunication;
using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Services.Sms;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatRumi.Account.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddMediator(config =>
            {
                config.Assemblies = [typeof(IRefMarker)];
                config.ServiceLifetime = ServiceLifetime.Scoped;
            });
            services.AddValidatorsFromAssembly(typeof(IRefMarker).Assembly);

            services.AddOptions<SmsOfficeOptions>().BindConfiguration(SmsOfficeOptions.Name);
            services.AddOptions<RedisOptions>().BindConfiguration(RedisOptions.Name);
            services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);

            services.AddScoped<ISmsService, SmsOfficeService>();
            services.AddHttpClient<ISmsService, SmsOfficeService>((sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<SmsOfficeOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl);
            });
            services.AddSingleton<IDispatcher, KafkaProducer>();
        }
    }
}