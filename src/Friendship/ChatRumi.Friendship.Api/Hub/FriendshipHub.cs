using ChatRumi.Friendship.Application.Services;
using ChatRumi.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Friendship.Api.Hub;

public class FriendshipHub(FriendshipConnectionManager friendshipConnectionManager) : Hub<IFriendshipHub>
{
    public override Task OnConnectedAsync()
    {
        if (TryGetAccount(out var accountId))
        {
            friendshipConnectionManager.AddAccount(accountId, Context.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetAccount(out var accountId))
        {
            friendshipConnectionManager.RemoveConnection(accountId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    private bool TryGetAccount(out Guid accountId)
    {
        return Context.User.TryGetAccountId(out accountId);
    }
}