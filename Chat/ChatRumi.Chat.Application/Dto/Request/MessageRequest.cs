namespace ChatRumi.Chat.Application.Dto.Request;

public record MessageRequest(
    Guid SenderId,
    string Content,
    Guid? ReplyOf
);