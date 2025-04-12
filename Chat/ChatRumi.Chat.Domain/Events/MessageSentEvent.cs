using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageSentEvent(
    Guid ConversationId,
    Guid SenderId,
    string Content,
    Guid? ReplyOf
) : DomainEvent
{
    public Message AsMessage()
    {
        return new Message
        {
            Id = Id,
            Content = new PlainTextContent
            {
                Content = Content
            },
            ConversationId = ConversationId,
            ParticipantId = SenderId,
            Sent = MessageType.Sent()
        };
    }

    public LatestMessage AsLatestMessage()
    {
        return new LatestMessage
        {
            Id = Id,
            Content = Content,
            ConversationId = ConversationId,
            ParticipantId = SenderId
        };
    }
}