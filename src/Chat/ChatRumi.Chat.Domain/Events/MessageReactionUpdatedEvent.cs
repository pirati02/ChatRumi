using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageReactionUpdatedEvent(
    Guid ChatId,
    Guid MessageId,
    Participant Actor,
    string Emoji
) : DomainEvent;
