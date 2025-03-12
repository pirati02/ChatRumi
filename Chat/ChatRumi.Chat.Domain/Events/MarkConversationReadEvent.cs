using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MarkConversationReadEvent : DomainEvent
{
    public required Guid[] MessageIds { get; set; }
}