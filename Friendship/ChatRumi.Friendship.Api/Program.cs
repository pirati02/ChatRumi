using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Dto.Response;
using ChatRumi.Friendship.Application.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi()
    .AddApplication();

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

app.MapGet("{peerId:guid}/requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetFriendRequestsAsync(peerId)))
    .WithName("friend-request")
    .WithOpenApi();

app.MapGet("{peerId:guid}/my-requests",
        async (Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        Results.Ok(await connectionManager.GetRequestsISent(peerId)))
    .WithName("my-friend-request")
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

await app.RunAsync();