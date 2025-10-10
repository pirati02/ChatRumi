using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageStatusChangeEvent : DomainEvent
{
    public required Guid MessageId { get; init; }
    public required Participant SenderId { get; set; }
    public required MessageStatus Status { get; init; }
}