using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageResponse(
    Guid ChatId,
    Guid MessageId,
    MessageStatus Status,
    MessageContent Content,
    ParticipantDto Sender,
    Guid? ReplyOf,
    MessageReactionResponse[] Reactions
);