using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageStatusChangeEvent : DomainEvent
{
    public required Guid MessageId { get; set; }
    public required Guid SenderId { get; set; }
    public required MessageStatus Status { get; set; }
}