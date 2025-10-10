namespace ChatRumi.Chat.Domain.ValueObject;

public record struct MessageType
{
    public DateTimeOffset Timestamp { get; set; }

    public static MessageType Delivered()
    {
        return new MessageType
        {
            Timestamp = DateTimeOffset.UtcNow
        };
    }
    
    public static MessageType Sent()
    {
        return new MessageType
        {
            Timestamp = DateTimeOffset.UtcNow
        };
    }
    
    public static MessageType Seen()
    {
        return new MessageType
        {
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}