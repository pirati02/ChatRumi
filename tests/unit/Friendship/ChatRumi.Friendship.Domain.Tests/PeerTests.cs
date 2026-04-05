using ChatRumi.Friendship.Domain.Aggregates;
using Xunit;

namespace ChatRumi.Friendship.Domain.Tests;

public class PeerTests
{
    [Fact]
    public void Peer_HoldsUserNameAndCreatedDate()
    {
        var created = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var peer = new Peer
        {
            UserName = "friend",
            CreatedDate = created
        };

        Assert.Equal("friend", peer.UserName);
        Assert.Equal(created, peer.CreatedDate);
    }
}
