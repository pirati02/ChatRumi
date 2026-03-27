using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Services;

namespace ChatRumi.Friendship.AccountSync;

public sealed class FriendshipHubContextProxy : IFriendshipHubContextProxy
{
    public Task FriendRequestReceived(PeerDto toPeer, PeerDto fromPeer) => Task.CompletedTask;

    public Task FriendRequestAccepted(PeerDto toPeer, PeerDto fromPeer) => Task.CompletedTask;
}
