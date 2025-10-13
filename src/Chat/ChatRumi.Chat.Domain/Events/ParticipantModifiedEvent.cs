using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record ParticipantModifiedEvent(
    Guid ParticipantId,
    string UserName,
    string FirstName,
    string LastName
) : DomainEvent;