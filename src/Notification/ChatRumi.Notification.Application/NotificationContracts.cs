namespace ChatRumi.Notification.Application;

public sealed record NotificationListItem(
    Guid Id,
    Guid RecipientId,
    Guid ActorId,
    string ActorDisplayName,
    string? ActorAvatarUrl,
    string Type,
    Guid TargetId,
    string? TargetPreview,
    string? Reaction,
    DateTimeOffset CreatedAt,
    bool IsRead
);

public sealed record NotificationPage(
    IReadOnlyList<NotificationListItem> Items,
    DateTimeOffset? NextCursor
);

public sealed record NotificationTriggered(
    Guid RecipientId,
    Guid ActorId,
    string ActorFirstName,
    string ActorLastName,
    string? ActorNickName,
    string Type,
    Guid TargetId,
    string? TargetPreview,
    string? Reaction,
    DateTimeOffset CreatedAt
);

public interface INotificationRealtimePublisher
{
    Task NotifyCreatedAsync(NotificationListItem notification, CancellationToken cancellationToken);
    Task NotifyUnreadCountChangedAsync(Guid recipientId, long unreadCount, CancellationToken cancellationToken);
}

public interface INotificationService
{
    Task<NotificationListItem?> CreateFromEventAsync(NotificationTriggered notificationTriggered, CancellationToken cancellationToken);
    Task<NotificationPage> GetPageAsync(Guid recipientId, DateTimeOffset? cursor, int pageSize, CancellationToken cancellationToken);
    Task<long> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken);
    Task<bool> MarkReadAsync(Guid recipientId, Guid notificationId, CancellationToken cancellationToken);
    Task<int> MarkAllReadAsync(Guid recipientId, CancellationToken cancellationToken);
}
