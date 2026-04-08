namespace ChatRumi.Friendship.Application.IntegrationEvents;

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
