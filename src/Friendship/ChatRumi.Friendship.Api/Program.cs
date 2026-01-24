using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Dto.Response;
using ChatRumi.Friendship.Application.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPresentation()
    .AddApplication();
builder.Services.AddConsulService(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("CorsPolicy");
app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("friendship-health");

var friendship = app.MapGroup("/api/friendship");

friendship.MapGet("{peerId:guid}",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendsAsync(peerId)))
    .WithName("friends");

friendship.MapGet("{peerId:guid}/received-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendRequestsAsync(peerId)))
    .WithName("received-requests");

friendship.MapGet("{peerId:guid}/sent-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetRequestsISent(peerId)))
    .WithName("sent-requests");

friendship.MapPost("request", async (
        [FromBody] InviteFriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        await connectionManager.SendFriendRequestAsync(request.Peer1, request.Peer2);
        return Results.NoContent();
    })
    .WithName("invite-friend");

friendship.MapPost("accept", async (
        [FromBody] AcceptFriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        await connectionManager.AcceptFriendRequestAsync(request.Peer1, request.Peer2);
        return Results.NoContent();
    })
    .WithName("accept-friend");


friendship.MapDelete("unfriend", async (
        [FromBody] UnfriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        await connectionManager.UnfriendAsync(request.Peer1, request.Peer2);
        return Results.NoContent();
    })
    .WithName("unfriend");

await app.RunAsync();

