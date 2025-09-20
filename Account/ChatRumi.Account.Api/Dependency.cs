using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Events; 
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
                policyBuilder.WithOrigins("http://localhost:4200") // Angular frontend URL
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Important for SignalR
            });
        });
        services.AddMediatR(config =>
            config.RegisterServicesFromAssemblies(ChatRumi.Account.Application.Application.Assembly));
        services.AddValidatorsFromAssembly(ChatRumi.Account.Application.Application.Assembly);

        services.AddMarten(options =>
        {
            options.Connection(configuration.GetConnectionString("Marten")!);
            options.UseSystemTextJsonForSerialization();
            if (environment.IsDevelopment())
            {
                options.AutoCreateSchemaObjects = AutoCreate.All;
            }

            options.Projections.Add<AccountProjectionTransform>(ProjectionLifecycle.Inline);
            options.Projections.LiveStreamAggregation<Domain.Aggregate.Account>();
            options.Schema.For<AccountProjection>()
                .UniqueIndex(x => x.UserName)
                .UniqueIndex(x => x.Email);

            options.Events.StreamIdentity = StreamIdentity.AsGuid;
        }).AddAsyncDaemon(DaemonMode.Solo);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<VerifyAccount.EventHandler>();

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
                        e.ConfigureConsumer<VerifyAccount.EventHandler>(
                            context);
                    });
            });
        });
        services.AddScoped<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { { options.Host, options.Port } },
                User = options.User,
                Password = options.Password
            });
        });
    }
}