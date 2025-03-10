using ChatRumi.Chat.Application.Dto.Response;

namespace ChatRumi.Chat.Application.Hubs;

public interface IConversationClient
{
    Task ConversationStarted(Guid conversationId);
    Task MessageSent(Guid conversationId, MessageResponse message);
}