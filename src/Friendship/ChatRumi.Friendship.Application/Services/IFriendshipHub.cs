using ChatRumi.Friendship.Application.Dto.Request;

namespace ChatRumi.Friendship.Application.Services;

public interface IFriendshipHub
{
    Task FriendRequestReceived(PeerDto toPeerId, PeerDto fromPeerId);
    Task FriendRequestAccepted(PeerDto toPeerId, PeerDto fromPeerId);
}