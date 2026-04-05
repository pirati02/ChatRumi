using System.Text.Json;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Chat.Infrastructure.Options;
using ChatRumi.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using StackExchange.Redis;
using ChatRumi.Chat.Application.Projections.LatestChat;

namespace ChatRumi.Chat.Infrastructure;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure(
            IConfiguration configuration,
            IWebHostEnvironment environment
        )
        {
            DbInitializer.Initialize(configuration.GetConnectionString("chatDatabase")!);
            services.AddMarten(configuration, environment, DefaultJsonContentOptions.CreateJsonOptions());
            services.AddRedis();
        }

        private void AddMarten(
            IConfiguration configuration,
            IHostEnvironment environment,
            JsonSerializerOptions jsonOptions
        )
        {
            services.AddMarten(options =>
                {
                    options.DisableNpgsqlLogging = true;
                    options.Connection(configuration.GetConnectionString("chatDatabase")!);
                    options.UseSystemTextJsonForSerialization(jsonOptions);

                    if (environment.IsDevelopment() || environment.IsEnvironment("Integration"))
                    {
                        options.AutoCreateSchemaObjects = AutoCreate.All;
                    }

                    options.Projections.LiveStreamAggregation<Domain.Aggregates.Chat>();
                    options.Projections.Add<ExistingChatProjectionTransform>(ProjectionLifecycle.Inline);
                    options.Schema.For<ExistingChatProjection>();

                    options.Projections.Add<LatestChatProjectionTransform>(ProjectionLifecycle.Inline);
                    options.Schema.For<LatestChatProjection>();

                    options.Events.StreamIdentity = StreamIdentity.AsGuid;
                })
                .AddAsyncDaemon(DaemonMode.Solo);
        }

        private void AddRedis()
        {
            services.AddScoped<IConnectionMultiplexer>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
                return ConnectionMultiplexer.Connect(new ConfigurationOptions
                {
                    EndPoints = { { options.Host, options.Port } },
                    User = options.User,
                    Password = options.Password,
                    AbortOnConnectFail = false
                });
            });
        }
    }
}
