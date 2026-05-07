namespace Skoleoverblikket.Api.Storage;

public interface IObjectStorage
{
    Task UploadAsync(string key, string contentType, Stream content, CancellationToken ct = default);

    Task<string> UploadPublicAsync(string key, string contentType, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Returns a presigned PUT URL the client can use to upload directly to S3,
    /// and the public URL the file will have after upload.
    /// </summary>
    Task<(string UploadUrl, string PublicUrl)> GeneratePresignedUploadUrlAsync(
        string key, string contentType, long contentLength, TimeSpan expiry, CancellationToken ct = default);
}
