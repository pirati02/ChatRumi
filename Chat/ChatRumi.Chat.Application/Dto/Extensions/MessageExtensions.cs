using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class MessageExtensions
{
    public static MessageResponse ToDto(this Message message)
    {
        return new MessageResponse(
            message.ChatId,
            message.Id,
            message.LatestStatus(),
            message.Content.Content,
            message.Participant.ToDto(),
            message.ReplyOf?.Id
        );
    }
}