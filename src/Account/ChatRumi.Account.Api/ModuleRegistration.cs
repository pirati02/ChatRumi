using ChatRumi.Account.Application.Events;
using MassTransit;

namespace ChatRumi.Account.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation(IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder =>
                {
                    policyBuilder.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            services.AddMassTransit(x =>
            {
                x.AddConsumer<VerifyAccount.Handler>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(configuration.GetConnectionString("MassTransit_Url"), h =>
                    {
                        h.Username(configuration.GetConnectionString("MassTransit_User")!);
                        h.Password(configuration.GetConnectionString("MassTransit_Url")!);
                    });

                    cfg.ReceiveEndpoint("verify-account-event-queue",
                        e =>
                        {
                            e.ConfigureConsumer<VerifyAccount.Handler>(
                                context);
                        });
                });
            });
        }
    }
}