using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Projections;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;

namespace ChatRumi.Account.Infrastructure;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure(IConfiguration configuration,
            IHostEnvironment environment
        )
        {
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
}