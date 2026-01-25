using System.Text.Json.Serialization;

namespace ChatRumi.Chat.Domain.ValueObject;

public record ImageContent : MessageContent
{
    [JsonIgnore]
    public Stream Image => new MemoryStream(Convert.FromBase64String(Content));
}