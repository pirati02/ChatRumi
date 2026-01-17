using ChatRumi.Friendship.Application.Dto.Request;

namespace ChatRumi.Friendship.Application.Dto.Response;

public record AcceptFriendRequest(PeerDto Peer1, PeerDto Peer2);