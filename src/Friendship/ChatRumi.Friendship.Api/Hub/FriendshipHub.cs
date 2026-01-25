using ChatRumi.Friendship.Application.Services;
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
        accountId = Guid.Empty;

        return Context.GetHttpContext()?.Request.Query.TryGetValue("accountId", out var values) == true &&
               Guid.TryParse(values, out accountId);
    }
}