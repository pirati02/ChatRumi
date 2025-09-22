using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageSentEvent(
    Guid ChatId,
    Participant SenderId,
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
            ChatId = ChatId,
            Participant = SenderId,
            Sent = MessageType.Sent()
        };
    }

    public LatestMessage AsLatestMessage()
    {
        return new LatestMessage
        {
            Id = Id,
            Content = Content,
            ChatId = ChatId,
            Participant = SenderId
        };
    }
}