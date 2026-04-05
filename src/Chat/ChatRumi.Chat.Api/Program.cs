using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.Api;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Infrastructure;
using ChatRumi.Infrastructure;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseChatRumiHttpsRedirectionAndHsts();
app.UseChatRumiSecurityHeaders();
app.UseResponseCompression();
// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("chat-health")
    .AllowAnonymous();

var chatGroup = app.MapGroup("/api/chat").RequireAuthorization();

chatGroup.MapGet("/{participantId:guid}/top10", async (
    HttpContext http,
    [FromRoute] Guid participantId,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId) || callerId != participantId)
    {
        return Results.Forbid();
    }

    var result = await mediator.Send(new GetTop10LatestChat.Query(participantId));
    return result.Match(Results.Ok, Results.NotFound);
});

chatGroup.MapGet("{chatId:guid}", async (
    HttpContext http,
    [FromRoute] Guid chatId,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var result = await mediator.Send(new GetChatById.Query(chatId, callerId));
    return result.Match(
        Results.Ok,
        errors => errors.Any(e => e.Type == ErrorType.Forbidden) ? Results.Forbid() : Results.NotFound());
});

chatGroup.MapPost("/search-existing", async (
    HttpContext http,
    [FromBody] ParticipantDto[] participants,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var result = await mediator.Send(new SearchExistingChatByParticipant.Query(participants, callerId));
    return result.Match(
        Results.Ok,
        errors => errors.Any(e => e.Type == ErrorType.Forbidden) ? Results.Forbid() : Results.NotFound());
});

chatGroup.MapPost("/mark-as-read/{chatId:guid}", async (
    HttpContext http,
    [FromRoute] Guid chatId,
    [FromBody] Guid[] messageIds,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var result = await mediator.Send(new MarkChatRead.Command(chatId, messageIds, callerId));
    return result.Match(
        Results.Ok,
        errors => errors.Any(e => e.Type == ErrorType.Forbidden) ? Results.Forbid() : Results.NotFound());
});

app.MapHub<ChatHub>("/hub/chat").RequireAuthorization();

await app.RunAsync();