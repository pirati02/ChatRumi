using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Friendship.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Friendship.Api.Hub;

public class FriendshipHubContextProxy(
    IHubContext<FriendshipHub, IFriendshipHub> hubContext,
    FriendshipConnectionManager friendshipConnectionManager
) : IFriendshipHubContextProxy
{
    public Task FriendRequestReceived(PeerDto toPeer, PeerDto fromPeer)
    {
        var connections = friendshipConnectionManager.GetConnections([toPeer.PeerId, fromPeer.PeerId]);
        return hubContext.Clients.Clients(connections).FriendRequestReceived(toPeer, fromPeer);
    }

    public Task FriendRequestAccepted(PeerDto toPeer, PeerDto fromPeer)
    {
        var connections = friendshipConnectionManager.GetConnections([toPeer.PeerId, fromPeer.PeerId]);
        return hubContext.Clients.Clients(connections).FriendRequestAccepted(toPeer, fromPeer);
    }
}