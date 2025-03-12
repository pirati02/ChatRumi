using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Hubs;

public interface IConversationClient
{
    Task ConversationStarted(Guid conversationId);
    Task MessageSent(Guid conversationId, MessageResponse message, bool updatestate);
    Task MessageStateUpdated(Guid conversationId, Guid messageId, MessageStatus status);
}