using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageResponse(
    Guid ChatId,
    Guid MessageId,
    MessageStatus Status,
    string Content,
    ParticipantDto Sender,
    Guid? ReplyOf
)
{
    public static MessageResponse From(Message m)
    {
        return new MessageResponse(
            m.ChatId,
            m.Id,
            m.LatestStatus(),
            m.Content.Content,
            m.Participant.ToDto(),
            m.ReplyOf?.Id
        );
    }
}