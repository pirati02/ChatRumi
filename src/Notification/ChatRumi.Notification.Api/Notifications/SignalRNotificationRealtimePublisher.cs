using ChatRumi.Notification.Api.Hub;
using ChatRumi.Notification.Application;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Notification.Api.Notifications;

public sealed class SignalRNotificationRealtimePublisher(
    IHubContext<NotificationHub, INotificationClient> hubContext,
    NotificationConnectionManager connectionManager
) : INotificationRealtimePublisher
{
    public async Task NotifyCreatedAsync(NotificationListItem notification, CancellationToken cancellationToken)
    {
        var connections = connectionManager.GetConnections(notification.RecipientId);
        if (connections.Count == 0)
        {
            return;
        }

        await hubContext.Clients.Clients(connections).NotificationCreated(notification);
    }

    public async Task NotifyUnreadCountChangedAsync(Guid recipientId, long unreadCount, CancellationToken cancellationToken)
    {
        var connections = connectionManager.GetConnections(recipientId);
        if (connections.Count == 0)
        {
            return;
        }

        await hubContext.Clients.Clients(connections).NotificationUnreadCountChanged(unreadCount);
    }
}
