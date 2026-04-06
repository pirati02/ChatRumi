using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;

namespace ChatRumi.Infrastructure.Storage;

public sealed class LocalAttachmentFileStorage(IHostEnvironment environment) : IAttachmentFileStorage
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public async Task<StoredAttachmentFile> StoreFileAsync(
        string bucket,
        string originalFileName,
        string? contentType,
        Stream content,
        long sizeBytes,
        CancellationToken cancellationToken = default
    )
    {
        var safeOriginalFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginalFileName);
        var attachmentId = Guid.CreateVersion7().ToString();
        var storageName = string.IsNullOrWhiteSpace(extension)
            ? attachmentId
            : $"{attachmentId}{extension}";

        var bucketRoot = GetBucketRoot(bucket);
        Directory.CreateDirectory(bucketRoot);

        var storagePath = Path.Combine(bucketRoot, storageName);
        await using (var output = File.Create(storagePath))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        var resolvedContentType = ResolveContentType(storageName, contentType);
        return new StoredAttachmentFile(
            attachmentId,
            safeOriginalFileName,
            storageName,
            resolvedContentType,
            sizeBytes
        );
    }

    public Task<StoredAttachmentReadResult?> GetFileAsync(
        string bucket,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return Task.FromResult<StoredAttachmentReadResult?>(null);
        }

        var storagePath = Path.Combine(GetBucketRoot(bucket), safeFileName);
        if (!File.Exists(storagePath))
        {
            return Task.FromResult<StoredAttachmentReadResult?>(null);
        }

        var stream = File.OpenRead(storagePath);
        return Task.FromResult<StoredAttachmentReadResult?>(
            new StoredAttachmentReadResult(
                safeFileName,
                ResolveContentType(safeFileName, contentType: null),
                stream
            )
        );
    }

    public Task<StoredAttachmentReadResult?> GetFileByPrefixAsync(
        string bucket,
        string fileNamePrefix,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safePrefix = Path.GetFileName(fileNamePrefix);
        if (string.IsNullOrWhiteSpace(safePrefix))
        {
            return Task.FromResult<StoredAttachmentReadResult?>(null);
        }

        var bucketRoot = GetBucketRoot(bucket);
        if (!Directory.Exists(bucketRoot))
        {
            return Task.FromResult<StoredAttachmentReadResult?>(null);
        }

        var matchedFilePath = Directory.EnumerateFiles(bucketRoot, $"{safePrefix}*").FirstOrDefault();
        if (matchedFilePath is null)
        {
            return Task.FromResult<StoredAttachmentReadResult?>(null);
        }

        var safeFileName = Path.GetFileName(matchedFilePath);
        var stream = File.OpenRead(matchedFilePath);
        return Task.FromResult<StoredAttachmentReadResult?>(
            new StoredAttachmentReadResult(
                safeFileName,
                ResolveContentType(safeFileName, contentType: null),
                stream
            )
        );
    }

    private string GetBucketRoot(string bucket)
    {
        var safeBucket = Path.GetFileName(bucket);
        if (string.IsNullOrWhiteSpace(safeBucket))
        {
            throw new ArgumentException("Storage bucket is required.", nameof(bucket));
        }

        return Path.Combine(environment.ContentRootPath, "uploads", safeBucket);
    }

    private static string ResolveContentType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return ContentTypeProvider.TryGetContentType(fileName, out var resolvedContentType)
            ? resolvedContentType
            : "application/octet-stream";
    }
}
