using System.Text.Json;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Chat.Application.Services;
using ChatRumi.Chat.Infrastructure.AccountPublicKey;
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
            services.Configure<AccountServiceOptions>(configuration.GetSection(AccountServiceOptions.SectionName));
            // BaseUrl: AccountService:BaseUrl, else ConnectionStrings:accountservice (Aspire WithReference from Chat to Account).
            services.AddHttpClient<IAccountPublicKeyProvider, AccountPublicKeyProvider>((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<AccountServiceOptions>>().Value;
                var baseUrl = opts.BaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                    baseUrl = configuration.GetConnectionString("accountservice") ?? "";
                if (!string.IsNullOrWhiteSpace(baseUrl))
                    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
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