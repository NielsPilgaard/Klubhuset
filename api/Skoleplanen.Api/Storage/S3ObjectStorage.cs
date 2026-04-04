using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using System.Net;

namespace Skoleplanen.Api.Storage;

public sealed class S3ObjectStorage(IAmazonS3 s3, IOptions<S3Options> opts) : IObjectStorage
{
    private readonly S3Options _options = opts.Value;

    public async Task<string> UploadAsync(string key, string contentType, Stream content, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.DefaultBucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
        };

        await s3.PutObjectAsync(request, ct);

        // URL-encode the key to ensure reserved/special characters are properly escaped
        var encodedKey = WebUtility.UrlEncode(key.TrimStart('/'));
        return $"{_options.PublicEndpoint.TrimEnd('/')}/{_options.DefaultBucketName}/{encodedKey}";
    }
}
