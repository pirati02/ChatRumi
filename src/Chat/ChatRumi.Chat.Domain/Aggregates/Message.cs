using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Aggregates;

// ReSharper disable once ClassNeverInstantiated.Global
public class Message : Aggregate
{
    public Guid ChatId { get; init; }
    public Participant Participant { get; init; } = null!;
    public required MessageContent Content { get; init; }
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