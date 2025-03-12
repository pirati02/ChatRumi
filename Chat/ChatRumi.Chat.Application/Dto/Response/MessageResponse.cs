using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageResponse(
    Guid ConversationId,
    Guid MessageId,
    MessageStatus Status,
    string Content,
    Guid SenderId,
    Guid? ReplyOf);