using ChatRumi.Notification.Application;

namespace ChatRumi.Notification.Api.Hub;

public interface INotificationClient
{
    Task NotificationCreated(NotificationListItem notification);
    Task NotificationUnreadCountChanged(long unreadCount);
}
