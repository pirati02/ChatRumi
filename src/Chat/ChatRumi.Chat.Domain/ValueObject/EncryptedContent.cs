namespace ChatRumi.Chat.Domain.ValueObject;

public record EncryptedContent : MessageContent
{
    public required string Iv { get; init; }
    public required Dictionary<Guid, string> EncryptedKeys { get; init; }
}