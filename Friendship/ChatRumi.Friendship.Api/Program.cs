using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Dto.Response;
using ChatRumi.Friendship.Application.Services;
using Consul;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi()
    .AddApplication();
builder.Services.AddConsulService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.MapGet("{peerId:guid}",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendsAsync(peerId)))
    .WithName("friends")
    .WithOpenApi();

app.MapGet("{peerId:guid}/received-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendRequestsAsync(peerId)))
    .WithName("received-requests")
    .WithOpenApi();

app.MapGet("{peerId:guid}/sent-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetRequestsISent(peerId)))
    .WithName("sent-requests")
    .WithOpenApi();

app.MapPut("{peerId:guid}/request", async (
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

app.MapPut("{peerId:guid}/accept", async (
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


app.MapDelete("{peerId:guid}/unfriend", async (
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

app.MapGet("/health", () => Results.Ok("Healthy ✅"));

await app.RunAsync();