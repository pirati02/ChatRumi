using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class ChatExtensions
{
    extension(Domain.Aggregates.Chat chat)
    {
        public ChatResponse ToDto()
        {
            return new ChatResponse(
                chat.Id,
                chat.Participants
                    .Select(p => p.ToDto())
                    .ToArray(),
                chat.Messages
                    .Select(m => m.ToDto())
                    .ToArray(),
                chat.Creator.ToDto(),
                chat.CreationDate
            );
        }
    }

    extension(ChatResponse chat)
    {
        public Guid[] Receivers(MessageRequest message)
        {
            var sender = message.Sender.Id;
            return
            [
                .. chat.Participants.Where(a => a.Id != sender)
                    .Select(p => p.Id)
            ];
        }
    }
}