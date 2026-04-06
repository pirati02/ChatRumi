using ChatRumi.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Notification.Api.Hub;

public sealed class NotificationHub(NotificationConnectionManager connectionManager) : Hub<INotificationClient>
{
    public override Task OnConnectedAsync()
    {
        if (TryGetAccount(out var accountId))
        {
            connectionManager.AddAccount(accountId, Context.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetAccount(out var accountId))
        {
            connectionManager.RemoveConnection(accountId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    private bool TryGetAccount(out Guid accountId)
    {
        return Context.User.TryGetAccountId(out accountId);
    }
}
