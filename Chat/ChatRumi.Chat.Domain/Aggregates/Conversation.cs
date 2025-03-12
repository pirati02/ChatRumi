using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Aggregates;

public record Conversation : Aggregate
{
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public List<Message> Messages { get; set; } = [];
    public Guid ParticipantId1 { get; set; }
    public Guid ParticipantId2 { get; set; }

    public void Apply(ConversationStartedEvent @event)
    {
        CreationDate = @event.Timestamp;
        ParticipantId1 = @event.ParticipantId1;
        ParticipantId2 = @event.ParticipantId2;
    }

    public void Apply(MessageSentEvent @event)
    {
        Messages.Add(
            new Message
            {
                Id = @event.Id,
                Content = new PlainTextContent
                {
                    Content = @event.Content
                },
                ConversationId = Id,
                ParticipantId = @event.SenderId,
                Sent = MessageType.Sent()
            }
        );
    }

    public void Apply(MessageStatusChangeEvent @event)
    {
        var message = Messages.FirstOrDefault(m => m.Id == @event.MessageId);
        if (message is null)
        {
            return;
        }

        switch (@event.Status)
        {
            case MessageStatus.Sent:
                message.Sent = MessageType.Sent();
                break;
            case MessageStatus.Delivered:
                message.Delivered = MessageType.Delivered();
                break;
            case MessageStatus.Seen:
                message.Seen = MessageType.Seen();
                break;
        }
    }

    public void Apply(MarkConversationReadEvent @event)
    {
        var messages = Messages.Where(a => @event.MessageIds.Contains(a.Id));
        foreach (var message in messages)
        {
            message.Delivered = MessageType.Delivered();
            message.Seen = MessageType.Seen();
        }
    }
}