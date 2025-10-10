using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Domain.Aggregates;

public sealed class LatestMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ChatId { get; set; }
    public Participant Participant { get; init; } = null!;
    public MessageContent Content { get; init; } = null!;
}