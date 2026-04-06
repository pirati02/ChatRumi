namespace ChatRumi.Chat.Domain.ValueObject;

public record MessageReaction
{
    public required Guid ActorId { get; init; }
    public required string Emoji { get; init; }
}
