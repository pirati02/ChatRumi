using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record ConversationStartedEvent: DomainEvent
{
    public required Guid ParticipantId1 { get; set; }
    public required Guid ParticipantId2 { get; set; }
};