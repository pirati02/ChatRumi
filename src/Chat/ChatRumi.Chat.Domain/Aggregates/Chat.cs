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
    public string Name { get; set; } = null!;
    public Participant Creator { get; private set; } = null!;

    public bool IsGroupChat => Participants.Count > 2;

    protected Chat()
    {
    }

    public Chat(
        string chatName,
        Participant creator,
        List<Participant> participants
    )
    {
        Fire(new ChatStartedEvent
        {
            Id = Id,
            Participants = participants,
            Creator = creator,
            ChatName = chatName
        });
    }

    public void Apply(ParticipantModifiedEvent @event)
    {
        var existing = Participants.FirstOrDefault(p => p.Id == @event.ParticipantId);
        if (existing is null)
            return;
        
        var updated = existing with
        {
            FirstName = @event.FirstName,
            LastName = @event.LastName,
            NickName = @event.UserName,
            PublicKey = @event.PublicKey ?? existing.PublicKey
        };
        
        ReplaceParticipant(existing, updated);
    }

    public void Apply(ChatStartedEvent @event)
    {
        CreationDate = @event.Timestamp;
        Participants = @event.Participants;
        Name = @event.ChatName;
        Creator = @event.Creator;
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

    public void Apply(MessageSentEvent @event)
    {
        Messages.Add(@event.AsMessage());
    }
      
    private void ReplaceParticipant(Participant oldParticipant, Participant updated)
    {
        var index = Participants.IndexOf(oldParticipant);
        if (index < 0) return;
        Participants[index] = updated;
    }

    private static void UpdateMessageStatus(Message message, MessageStatus status)
    {
        switch (status)
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
}