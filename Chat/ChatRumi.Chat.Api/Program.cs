using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Chat.Api;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi(builder.Configuration, builder.Environment);
builder.Services.AddConsulService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.MapGet("/{participantId:guid}/top10/", async (
    [FromRoute] Guid participantId,
    [FromQuery(Name = "Ids")] Guid[] responderParticipantIds,
    IMediator mediator
) =>
{
    var result = await mediator.Send(new GetTop10LatestConversation.Query(participantId, responderParticipantIds));
    return result.Match(Results.Ok, Results.NotFound);
});

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
app.MapGet("/health", () => Results.Ok("Healthy ✅"));

await app.RunAsync();