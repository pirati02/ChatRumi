using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class MessageExtensions
{
    extension(Message message)
    {
        public MessageResponse ToDto()
        {
            return new MessageResponse(
                message.ChatId,
                message.Id,
                message.LatestStatus(),
                message.Content,
                message.Participant.ToDto(),
                message.ReplyOf?.Id
            );
        }
    }
}