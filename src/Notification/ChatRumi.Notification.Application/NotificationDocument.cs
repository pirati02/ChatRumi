namespace ChatRumi.Notification.Application;

public sealed class NotificationDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid RecipientId { get; init; }
    public Guid ActorId { get; init; }
    public string ActorDisplayName { get; init; } = string.Empty;
    public string? ActorAvatarUrl { get; init; }
    public string Type { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string? TargetPreview { get; init; }
    public string? Reaction { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
