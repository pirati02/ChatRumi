using ChatRumi.Kernel;

namespace ChatRumi.Friendship.Domain.Events;

public record ConnectionCreateEvent: DomainEvent
{
    public Guid Peer1 { get; init; }
    public Guid Peer2 { get; init; }
}