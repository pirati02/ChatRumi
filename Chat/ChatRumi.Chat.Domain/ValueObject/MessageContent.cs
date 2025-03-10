namespace ChatRumi.Chat.Domain.ValueObject;

public abstract record MessageContent
{
    public required string Content { get; set; }
}

public record PlainTextContent : MessageContent;

public record LinkContent : MessageContent
{
    public Uri Link => new(Content);
}

public record ImageContent : MessageContent
{
    public Stream Image => new MemoryStream(Convert.FromBase64String(Content));
}