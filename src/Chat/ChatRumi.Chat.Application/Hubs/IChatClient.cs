using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Hubs;

public interface IChatClient
{
    Task ChatStarted(Guid chatId);
    Task MessageSent(MessageResponse message, bool updateState);
    Task MessageFailed(MessageResponse message);
    Task MessageStateUpdated(Guid messageId, MessageStatus status);
    Task MessageReactionUpdated(Guid messageId, MessageReactionResponse[] reactions);
}