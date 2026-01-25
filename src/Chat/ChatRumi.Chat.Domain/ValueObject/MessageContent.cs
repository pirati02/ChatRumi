using System.Text.Json.Serialization;

namespace ChatRumi.Chat.Domain.ValueObject;

[JsonDerivedType(typeof(PlainTextContent), typeDiscriminator: "plain")]
[JsonDerivedType(typeof(LinkContent), typeDiscriminator: "link")]
[JsonDerivedType(typeof(ImageContent), typeDiscriminator: "image")]
[JsonDerivedType(typeof(EncryptedContent), typeDiscriminator: "encrypted")]
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

/// <summary>
/// End-to-end encrypted message content.
/// Content contains the AES-encrypted message payload (Base64).
/// EncryptedKeys contains per-recipient encrypted AES keys.
/// </summary>
public record EncryptedContent : MessageContent
{
    /// <summary>
    /// Initialization vector for AES decryption (Base64 encoded)
    /// </summary>
    public required string Iv { get; init; }
    
    /// <summary>
    /// Dictionary of recipient ID to their encrypted AES key (Base64 encoded).
    /// Each recipient can decrypt their key using their private key.
    /// </summary>
    public required Dictionary<Guid, string> EncryptedKeys { get; init; }
}
