using System.Text.Json;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Chat.Application.Projections.LatestChat;
using ChatRumi.Chat.Infrastructure.Options;
using ChatRumi.Infrastructure;
using Marten.Events;
using Marten;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;

namespace ChatRumi.Chat.Infrastructure;

public static class Dependency
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        DbInitializer.Initialize(configuration.GetConnectionString("Marten")!);
        services.AddMarten(configuration, environment, DefaultJsonContentOptions.CreateJsonOptions());
        services.AddRedis();
    }

    private static void AddMarten(this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        JsonSerializerOptions jsonOptions)
    {
        services.AddMarten(options =>
            {
                options.DisableNpgsqlLogging = true;
                options.Connection(configuration.GetConnectionString("Marten")!);
                options.UseSystemTextJsonForSerialization(jsonOptions);

                if (environment.IsDevelopment())
                {
                    options.AutoCreateSchemaObjects = AutoCreate.All;
                }

                options.Projections.LiveStreamAggregation<Domain.Aggregates.Chat>();
                options.Projections.Add<ExistingChatProjectionTransform>(ProjectionLifecycle.Inline);
                options.Schema.For<ExistingChatProjection>();
            
                options.Events.StreamIdentity = StreamIdentity.AsGuid;
            })
            .AddAsyncDaemon(DaemonMode.Solo);
    }

    private static void AddRedis(this IServiceCollection services)
    {
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
