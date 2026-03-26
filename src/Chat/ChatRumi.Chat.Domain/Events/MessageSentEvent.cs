using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageSentEvent(
    Guid ChatId,
    Participant SenderId,
    MessageContent Content,
    Guid? ReplyOf
) : DomainEvent
{
    public Message AsMessage()
    {
        return new Message
        {
            Id = Id,
            Content = Content,
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
            Content = GetPlainText(Content),
            ChatId = ChatId,
            Participant = SenderId
        };
    }

    private static PlainTextContent GetPlainText(MessageContent messageContent)
    {
        return messageContent switch
        {
            PlainTextContent content => content,
            ImageContent => new PlainTextContent
            {
                Content = "picture attachment"
            },
            LinkContent content => new PlainTextContent
            {
                Content = content.Content
            },
            EncryptedContent => new PlainTextContent
            {
                Content = "🔒 encrypted message"
            },
            _ => throw new Exception("Invalid message content")
        };
    }
}