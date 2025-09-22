using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MarkChatReadEvent : DomainEvent
{
    public required Guid[] MessageIds { get; set; }
}