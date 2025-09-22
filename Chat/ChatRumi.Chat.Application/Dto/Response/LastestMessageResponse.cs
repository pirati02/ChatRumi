using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Dto.Response;

public record LatestMessageResponse(
    Guid ChatId,
    Guid MessageId,
    string? Content,
    ParticipantDto Sender
)
{
    public static LatestMessageResponse From(Guid chatId, LatestMessage m)
    {
        return new LatestMessageResponse(
            chatId,
            m.Id,
            m.Content,
            m.Participant.ToDto()
        );
    }
}