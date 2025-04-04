using ChatRumi.Friendship.Domain.Events;
using ChatRumi.Friendship.Domain.ValueObjects;
using ChatRumi.Kernel;

namespace ChatRumi.Friendship.Domain.Aggregates;

public record Connection : Aggregate
{
    public Guid Peer1 { get; private set; }
    public Guid Peer2 { get; private set; }
    public Guid RequestedById { get; private set; }
    public ConnectionState State { get; private set; }
 
    public void Apply(ConnectionCreateEvent @event)
    {
        Peer1 = @event.Peer1;
        Peer2 = @event.Peer2;
        RequestedById = @event.Peer1;
        State = ConnectionState.Requested;
    }
}