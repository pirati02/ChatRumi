using ChatRumi.Kernel;

namespace ChatRumi.Friendship.Domain.Aggregates;

public record Peer: Aggregate
{
    public required string UserName { get; set; }
    public DateTime CreatedDate { get; set; }
}