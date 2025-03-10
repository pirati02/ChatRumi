namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageResponse(
    Guid ConversationId,
    string Content,
    Guid SenderId,
    Guid? ReplyOf
);