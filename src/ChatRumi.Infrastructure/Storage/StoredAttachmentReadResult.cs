namespace ChatRumi.Infrastructure.Storage;

public sealed record StoredAttachmentReadResult(
    string FileName,
    string ContentType,
    Stream ContentStream
);
