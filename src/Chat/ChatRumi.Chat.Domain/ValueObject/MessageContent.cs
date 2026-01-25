namespace ChatRumi.Chat.Domain.ValueObject;

public abstract record MessageContent
{
    public required string Content { get; init; }
}