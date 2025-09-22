using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Options;
using ChatRumi.Chat.Application.Projections;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Chat.Application.Projections.LatestChat;
using ChatRumi.Infrastructure;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;

namespace ChatRumi.Chat.Api;

public static class Dependency
{
    public static void AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        DbInitializer.Initialize(configuration.GetConnectionString("Marten")!);
        
        services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder =>
                {
                    policyBuilder.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            })
            .AddMarten(options =>
            {
                options.DisableNpgsqlLogging = true;
                options.Connection(configuration.GetConnectionString("Marten")!);
                options.UseSystemTextJsonForSerialization();
                if (environment.IsDevelopment())
                {
                    options.AutoCreateSchemaObjects = AutoCreate.All;
                }

                options.Projections.LiveStreamAggregation<Domain.Aggregates.Chat>();
        
                options.Projections.Add<ExistingChatProjectionTransform>(ProjectionLifecycle.Inline);
                options.Schema.For<ExistingChatProjection>();

                options.Projections.Add<LatestChatProjectionTransform>(ProjectionLifecycle.Async);
                options.Schema.For<LatestChatProjection>();
        
                options.Events.StreamIdentity = StreamIdentity.AsGuid;
            })
            .AddAsyncDaemon(DaemonMode.Solo);

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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Application.Application.Assembly));
        
        services.AddSingleton<AccountConnectionManager>();
        services.AddSignalR();
        services.AddOpenApi();
    }
}