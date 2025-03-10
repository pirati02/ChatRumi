using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Options;
using ChatRumi.Chat.Application.Projections;
using ChatRumi.Chat.Domain.Aggregates;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Marten")!);
    options.UseSystemTextJsonForSerialization();
    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = AutoCreate.All;
    }

    options.Projections.Add<ExistingConversationProjectionTransform>(ProjectionLifecycle.Inline);
    options.Projections.LiveStreamAggregation<Conversation>();
    options.Schema.For<ExistingConversationProjection>();

    options.Events.StreamIdentity = StreamIdentity.AsGuid;
}).AddAsyncDaemon(DaemonMode.Solo);
builder.Services.AddScoped<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(new ConfigurationOptions
    {
        EndPoints = { { options.Host, options.Port } },
        User = options.User,
        Password = options.Password
    });
});

builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();
 
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapHub<ConversationHub>("/conversation");
app.Run();