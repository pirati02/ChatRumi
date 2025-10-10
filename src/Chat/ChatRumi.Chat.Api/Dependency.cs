using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Options;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Chat.Application.Projections.LatestChat;
using ChatRumi.Chat.Domain.ValueObject;
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
        });

        // Create one shared serializer options for Marten + ASP.NET
        var jsonOptions = CreateJsonOptions();

        services.AddMarten(configuration, environment, jsonOptions);
        services.AddRedis();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Application.Application.Assembly));
         
        services.AddSingleton<AccountConnectionManager>();
        services.AddScoped<IChatHubContextProxy, ChatHubContextProxy>();
        services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions = jsonOptions);
        services.AddOpenApi();
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
                options.Projections.Add<LatestChatProjectionTransform>(ProjectionLifecycle.Async);
                options.Schema.For<LatestChatProjection>();

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
 
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        options.TypeInfoResolverChain.Insert(0,
            new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    ti =>
                    {
                        if (ti.Type != typeof(MessageContent)) return;
                        ti.PolymorphismOptions = new JsonPolymorphismOptions
                        {
                            TypeDiscriminatorPropertyName = "type",
                            IgnoreUnrecognizedTypeDiscriminators = false
                        };
                        ti.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(PlainTextContent), "plain"));
                        ti.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(LinkContent), "link"));
                        ti.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(ImageContent), "image"));
                    }
                }
            });

        return options;
    }
}
