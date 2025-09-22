using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record ChatStartedEvent: DomainEvent
{
    public bool IsGroupChat { get; init; }
    public required List<Participant> Participants { get; init; } = null!;
};