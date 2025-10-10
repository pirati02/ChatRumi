using System.Text.Json.Serialization;

namespace ChatRumi.Chat.Domain.ValueObject;

[JsonDerivedType(typeof(PlainTextContent), typeDiscriminator: "plain")]
[JsonDerivedType(typeof(LinkContent), typeDiscriminator: "link")]
[JsonDerivedType(typeof(ImageContent), typeDiscriminator: "image")]
public abstract record MessageContent
{
    public required string Content { get; init; }
}

public record PlainTextContent : MessageContent;

public record LinkContent : MessageContent;

public record ImageContent : MessageContent
{
    [JsonIgnore]
    public Stream Image => new MemoryStream(Convert.FromBase64String(Content));
}