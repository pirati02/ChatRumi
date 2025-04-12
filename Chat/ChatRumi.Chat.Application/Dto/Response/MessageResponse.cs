using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Response;

public record MessageResponse(
    Guid ConversationId,
    Guid MessageId,
    MessageStatus Status,
    string Content,
    Guid SenderId,
    Guid? ReplyOf
)
{
    public static MessageResponse From(Message m)
    {
        return new MessageResponse(
            m.ConversationId,
            m.Id,
            m.LatestStatus(),
            m.Content.Content,
            m.ParticipantId,
            m.ReplyOf?.Id
        );
    }
}