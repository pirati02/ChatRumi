namespace ChatRumi.Chat.Application.Hubs;

public interface IConversationClient
{
    Task NewConversationCreated(Guid conversationId, Guid[] participants);
    void MessageSent(Guid conversationId, Guid messageId);
}