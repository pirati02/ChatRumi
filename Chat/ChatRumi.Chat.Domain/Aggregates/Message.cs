using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Domain.Aggregates;

public record Message
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ConversationId { get; set; }
    public Guid ParticipantId { get; set; }
    public required MessageContent Content { get; set; }
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public MessageType? Delivered { get; set; }
    public MessageType? Sent { get; set; }
    public MessageType? Seen { get; set; }
    public Message? ReplyOf { get; set; }

    public MessageStatus LatestStatus()
    {
        if (Seen is not null)
        {
            return MessageStatus.Seen;
        }

        if (Delivered is not null)
        {
            return MessageStatus.Delivered;
        }

        return MessageStatus.Sent;
    }
}