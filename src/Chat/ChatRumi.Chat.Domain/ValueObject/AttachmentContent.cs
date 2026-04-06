namespace ChatRumi.Chat.Domain.ValueObject;

public record AttachmentContent : MessageContent
{
    public required string FileName { get; init; }
    public required string MimeType { get; init; }
    public required long SizeBytes { get; init; }
}
