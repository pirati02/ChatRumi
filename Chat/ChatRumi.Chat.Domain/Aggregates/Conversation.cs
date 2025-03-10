using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Aggregates;

public record Conversation : Aggregate
{
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public List<Message> Messages { get; set; } = [];
    public List<Guid> Participants { get; set; } = [];

    public static Conversation Begin(Message message, Guid[] withParticipants)
    {
        var conversation = new Conversation();
        conversation.Fire(new ConversationStartedEvent
        {
            Message = message,
            ParticipantIds = withParticipants
        });
        return conversation;
    }

    public void Apply(ConversationStartedEvent @event)
    {
        CreationDate = @event.Timestamp;
        Participants = [.. @event.ParticipantIds];
        Messages = [@event.Message];
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
                Sent = new MessageType
                {
                    Timestamp = DateTimeOffset.Now
                }
            }
        );
    }
}