namespace ChatRumi.Infrastructure.Storage;

public interface IAttachmentFileStorage
{
    Task<StoredAttachmentFile> StoreFileAsync(
        string bucket,
        string originalFileName,
        string? contentType,
        Stream content,
        long sizeBytes,
        CancellationToken cancellationToken = default
    );

    Task<StoredAttachmentReadResult?> GetFileAsync(
        string bucket,
        string fileName,
        CancellationToken cancellationToken = default
    );

    Task<StoredAttachmentReadResult?> GetFileByPrefixAsync(
        string bucket,
        string fileNamePrefix,
        CancellationToken cancellationToken = default
    );
}
