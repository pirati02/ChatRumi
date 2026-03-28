using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Api.Hub;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Dto.Response;
using ChatRumi.Friendship.Application.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

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
    .WithName("friendship-health")
    .AllowAnonymous();

var friendship = app.MapGroup("/api/friendship").RequireAuthorization();

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

app.MapHub<FriendshipHub>("/hub/friendship").RequireAuthorization();

await app.RunAsync();

