using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Dto.Response;

public record LatestMessageResponse(
    Guid ConversationId,
    Guid MessageId,
    string? Content,
    Guid SenderId
)
{
    public static LatestMessageResponse From(Guid conversationId, LatestMessage m)
    {
        return new LatestMessageResponse(
            conversationId,
            m.Id,
            m.Content,
            m.ParticipantId
        );
    }
}