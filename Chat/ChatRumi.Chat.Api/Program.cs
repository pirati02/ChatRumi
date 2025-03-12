using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Projections;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Domain.Aggregates;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;
using RedisOptions = ChatRumi.Chat.Application.Options.RedisOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policyBuilder =>
        {
            policyBuilder.WithOrigins("http://localhost:4200") // Angular frontend URL
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Important for SignalR
        });
    })
    .AddMarten(options =>
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
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Application.Assembly));
builder.Services.AddSingleton<ConversationConnectionManager>();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.MapGet("/existing/{participantId1}/{participantId2}", async (
    [FromRoute] Guid participantId1,
    [FromRoute] Guid participantId2,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new GetConversationByParticipant.Query(participantId1, participantId2));
    return result.Match(Results.Ok, Results.NotFound);
});

app.MapPost("/mark-as-read/{conversationId:guid}", async (
    [FromRoute] Guid conversationId,
    [FromBody] Guid[] messageIds,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new MarkConversationRead.Command(conversationId, messageIds));
    return result.Match(Results.Ok, Results.NotFound);
});
app.MapHub<ConversationHub>("/conversation");
app.Run();