using ChatRumi.Kernel;

namespace ChatRumi.Friendship.Domain.Aggregates;

// ReSharper disable once ClassNeverInstantiated.Global
public class Peer : Aggregate
{
    public required string UserName { get; set; }
    public DateTime CreatedDate { get; set; }
}