namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageReactionResponse(
    Guid ActorId,
    string Emoji
);
