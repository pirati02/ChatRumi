using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record GroupChatStartedEvent: DomainEvent
{
    public required Guid[] Participants { get; init; }
};