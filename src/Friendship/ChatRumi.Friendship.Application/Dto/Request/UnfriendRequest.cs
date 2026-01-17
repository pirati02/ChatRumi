namespace ChatRumi.Friendship.Application.Dto.Request;

public record UnfriendRequest(PeerDto Peer1, PeerDto Peer2);