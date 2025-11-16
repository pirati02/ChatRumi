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
}