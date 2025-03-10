using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record ConversationStartedEvent: DomainEvent
{
    public required Message Message { get; set; }
    public required Guid[] ParticipantIds { get; set; }
};