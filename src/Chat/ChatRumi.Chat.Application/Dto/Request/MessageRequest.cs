using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Request;

public record MessageRequest(
    ParticipantDto Sender,
    MessageContent Content,
    Guid? ReplyOf
);