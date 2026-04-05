using ChatRumi.Account.Application.Documents;
using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Infrastructure;
using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ChatRumi.Account.Infrastructure;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure(IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            DbInitializer.Initialize(configuration.GetConnectionString("accountDatabase")!);
            services.AddMarten(options =>
            {
                options.Connection(configuration.GetConnectionString("accountDatabase")!);
                options.UseSystemTextJsonForSerialization();
                if (environment.IsDevelopment() || environment.IsEnvironment("Integration"))
                {
                    options.AutoCreateSchemaObjects = AutoCreate.All;
                }

                options.Projections.Add<AccountProjectionTransform>(ProjectionLifecycle.Inline);
                options.Projections.LiveStreamAggregation<Domain.Aggregate.Account>();
                options.Schema.For<AccountProjection>()
                    .UniqueIndex(x => x.UserName)
                    .UniqueIndex(x => x.Email);

                options.Schema.For<StoredRefreshToken>()
                    .UniqueIndex(x => x.TokenHash);

                options.Events.StreamIdentity = StreamIdentity.AsGuid;
            }).AddAsyncDaemon(DaemonMode.Solo);

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