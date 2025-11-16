using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Dto.Response;
using ChatRumi.Friendship.Application.Services;
using Consul;
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

var friendship = app.MapGroup("/api/friendship");

friendship.MapGet("{peerId:guid}",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendsAsync(peerId)))
    .WithName("friends")
    .WithOpenApi();

friendship.MapGet("{peerId:guid}/received-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendRequestsAsync(peerId)))
    .WithName("received-requests")
    .WithOpenApi();

friendship.MapGet("{peerId:guid}/sent-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetRequestsISent(peerId)))
    .WithName("sent-requests")
    .WithOpenApi();

friendship.MapPut("{peerId:guid}/request", async (
        Guid peerId,
        [FromBody] InviteFriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        await connectionManager.SendFriendRequestAsync(peerId, request.PeerId);
        return Results.Accepted();
    })
    .WithName("invite-friend")
    .WithOpenApi();

friendship.MapPut("{peerId:guid}/accept", async (
        Guid peerId,
        [FromBody] AcceptFriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        await connectionManager.AcceptFriendRequestAsync(peerId, request.PeerId);
        return Results.Accepted();
    })
    .WithName("accept-friend")
    .WithOpenApi();


friendship.MapDelete("{peerId:guid}/unfriend", async (
        Guid peerId,
        [FromBody] UnfriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        await connectionManager.UnfriendAsync(peerId, request.PeerId);
        return Results.Accepted();
    })
    .WithName("unfriend")
    .WithOpenApi();

friendship.MapGet("/health", () => Results.Ok("Healthy ✅"));

await app.RunAsync();