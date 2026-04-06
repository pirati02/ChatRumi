using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Api.Hub;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Dto.Response;
using ChatRumi.Friendship.Application.Services;
using ChatRumi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPresentation(builder.Configuration, builder.Environment);
builder.Services.AddApplication();

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
    .WithName("friendship-health")
    .AllowAnonymous();

var friendship = app.MapGroup("/api/friendship").RequireAuthorization();

friendship.MapGet("{peerId:guid}",
        async (HttpContext http, Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        {
            if (!http.User.TryGetAccountId(out var callerId) || callerId != peerId)
            {
                return Results.Forbid();
            }

            return Results.Ok(await connectionManager.GetFriendsAsync(peerId));
        })
    .WithName("friends");

friendship.MapGet("{peerId:guid}/received-requests",
        async (HttpContext http, Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        {
            if (!http.User.TryGetAccountId(out var callerId) || callerId != peerId)
            {
                return Results.Forbid();
            }

            return Results.Ok(await connectionManager.GetFriendRequestsAsync(peerId));
        })
    .WithName("received-requests");

friendship.MapGet("{peerId:guid}/sent-requests",
        async (HttpContext http, Guid peerId, [FromServices] IPeerConnectionManager connectionManager) =>
        {
            if (!http.User.TryGetAccountId(out var callerId) || callerId != peerId)
            {
                return Results.Forbid();
            }

            return Results.Ok(await connectionManager.GetRequestsISent(peerId));
        })
    .WithName("sent-requests");

friendship.MapPost("request", async (
        HttpContext http,
        [FromBody] InviteFriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Peer1.PeerId)
        {
            return Results.Forbid();
        }

        await connectionManager.SendFriendRequestAsync(request.Peer1, request.Peer2);
        return Results.NoContent();
    })
    .WithName("invite-friend");

friendship.MapPost("accept", async (
        HttpContext http,
        [FromBody] AcceptFriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Peer1.PeerId)
        {
            return Results.Forbid();
        }

        await connectionManager.AcceptFriendRequestAsync(request.Peer1, request.Peer2);
        return Results.NoContent();
    })
    .WithName("accept-friend");


friendship.MapDelete("unfriend", async (
        HttpContext http,
        [FromBody] UnfriendRequest request,
        [FromServices] IPeerConnectionManager connectionManager
    ) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Peer1.PeerId)
        {
            return Results.Forbid();
        }

        await connectionManager.UnfriendAsync(request.Peer1, request.Peer2);
        return Results.NoContent();
    })
    .WithName("unfriend");

app.MapHub<FriendshipHub>("/hub/friendship").RequireAuthorization();

await app.RunAsync();

