namespace ChatRumi.Friendship.Application.Dto.Request;

public record PeerResponse(
    Guid PeerId, 
    string UserName, 
    DateTime CreatedDate,
    string? PublicKey = null
);
