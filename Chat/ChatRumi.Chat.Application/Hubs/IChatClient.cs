using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Hubs;

public interface IChatClient
{
    Task ConversationStarted(Guid conversationId);
    Task MessageSent(MessageResponse message, bool updateState);
    Task MessageStateUpdated(Guid messageId, MessageStatus status);
}