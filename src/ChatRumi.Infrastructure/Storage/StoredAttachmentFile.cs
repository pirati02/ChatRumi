namespace ChatRumi.Infrastructure.Storage;

public sealed record StoredAttachmentFile(
    string AttachmentId,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes
);
