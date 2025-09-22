using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Aggregates;

// ReSharper disable once ClassNeverInstantiated.Global
public class Chat : Aggregate
{
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public List<Message> Messages { get; set; } = [];
    public List<Participant> Participants { get; private set; } = [];

    protected Chat()
    {
        
    }
    
    public Chat(bool isGroupChat, List<Participant> participants)
    {
        Fire(new ChatStartedEvent
        {
            Id = Id,
            IsGroupChat = isGroupChat,
            Participants = participants
        });
    }
    
    public void Apply(ChatStartedEvent @event)
    {
        CreationDate = @event.Timestamp;
        Participants = @event.Participants;
    }

    public void Apply(MessageSentEvent @event)
    {
        Messages.Add(@event.AsMessage());
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

    public void Apply(MarkChatReadEvent @event)
    {
        var messages = Messages.Where(a => @event.MessageIds.Contains(a.Id));
        foreach (var message in messages)
        {
            message.Delivered = MessageType.Delivered();
            message.Seen = MessageType.Seen();
        }
    }
}