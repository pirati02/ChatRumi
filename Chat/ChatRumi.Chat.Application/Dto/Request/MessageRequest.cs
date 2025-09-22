namespace ChatRumi.Chat.Application.Dto.Request;

public record MessageRequest(
    ParticipantDto Sender,
    string Content,
    Guid? ReplyOf
);