using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Chat.Api;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddApi();
builder.Services.AddConsulService(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("CorsPolicy");

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