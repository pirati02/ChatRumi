namespace ChatRumi.Chat.Application.Dto.Request;

public record MessageRequest(
    Guid SenderId,
    Guid ReceiverId,
    string Content,
    Guid? ReplyOf
);