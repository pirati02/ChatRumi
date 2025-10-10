using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Response;

public record LatestMessageResponse(
    Guid ChatId,
    Guid MessageId,
    MessageContent? Content,
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