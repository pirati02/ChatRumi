using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageResponse(
    Guid ChatId,
    Guid MessageId,
    MessageStatus Status,
    string Content,
    ParticipantDto Sender,
    Guid? ReplyOf
);