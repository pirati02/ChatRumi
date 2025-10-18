using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Events;
using ChatRumi.Infrastructure;
using FluentValidation;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using MassTransit;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;

namespace ChatRumi.Account.Api;

public static class Dependency
{
    public static void AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
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
                cfg.Host(configuration.GetConnectionString("MassTransit"), h =>
                {
                    h.Username("admin");
                    h.Password("rbadminpass");
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