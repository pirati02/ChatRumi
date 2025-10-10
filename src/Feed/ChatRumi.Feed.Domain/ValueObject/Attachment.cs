namespace ChatRumi.Feed.Domain.ValueObject;

public sealed class Attachment
{
    public AttachmentId Id { get; set; }
}

public readonly record struct AttachmentId(Guid Guid);