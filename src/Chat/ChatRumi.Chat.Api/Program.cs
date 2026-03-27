using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.Api;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPresentation();
builder.Services.AddConsulService(builder.Configuration);
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

var app = builder.Build();

// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

app.UseRouting();
app.UseCors("CorsPolicy");
app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("chat-health");

var chatGroup = app.MapGroup("/api/chat");

chatGroup.MapGet("/{participantId:guid}/top10", async (
    [FromRoute] Guid participantId,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new GetTop10LatestChat.Query(participantId));
    return result.Match(Results.Ok, Results.NotFound);
});

chatGroup.MapGet("{chatId:guid}", async (
    [FromRoute] Guid chatId,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new GetChatById.Query(chatId));
    return result.Match(Results.Ok, Results.NotFound);
});

chatGroup.MapPost("/search-existing", async (
    [FromBody] ParticipantDto[] participants,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new SearchExistingChatByParticipant.Query(participants));
    return result.Match(Results.Ok, Results.NotFound);
});

chatGroup.MapPost("/mark-as-read/{chatId:guid}", async (
    [FromRoute] Guid chatId,
    [FromBody] Guid[] messageIds,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new MarkChatRead.Command(chatId, messageIds));
    return result.Match(Results.Ok, Results.NotFound);
});

app.MapHub<ChatHub>("/hub/chat");

await app.RunAsync();