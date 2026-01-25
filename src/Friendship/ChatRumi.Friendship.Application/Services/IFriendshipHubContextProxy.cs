using ChatRumi.Friendship.Application.Dto.Request;

namespace ChatRumi.Friendship.Application.Services;

public interface IFriendshipHubContextProxy
{
    Task FriendRequestReceived(PeerDto toPeer, PeerDto fromPeer);
    Task FriendRequestAccepted(PeerDto toPeer, PeerDto fromPeer);
}