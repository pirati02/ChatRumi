using ChatRumi.Chat.Application.Dto.Response;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class ChatExtensions
{
    public static ChatResponse ToDto(this Domain.Aggregates.Chat chat)
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