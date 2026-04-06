namespace ChatRumi.Chat.Application.Dto.Request;

public record MessageReactionRequest(
    Guid MessageId,
    ParticipantDto Actor,
    string Emoji
);
