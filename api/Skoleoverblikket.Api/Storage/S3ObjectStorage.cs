using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using System.Net;

namespace Skoleoverblikket.Api.Storage;

public sealed class S3ObjectStorage(IAmazonS3 s3, IOptions<S3Options> opts) : IObjectStorage
{
	private readonly S3Options _options = opts.Value;

	public async Task UploadAsync(string key, string contentType, Stream content, CancellationToken ct = default)
	{
		var request = new PutObjectRequest
		{
			BucketName = _options.DefaultBucketName,
			Key = key,
			InputStream = content,
			ContentType = contentType,
			CannedACL = S3CannedACL.NoACL,
		};

		await s3.PutObjectAsync(request, ct);
	}

	public async Task<string> UploadPublicAsync(string key, string contentType, Stream content, CancellationToken ct = default)
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

		return BuildPublicUrl(key);
	}

	public Task<(string UploadUrl, string PublicUrl)> GeneratePresignedUploadUrlAsync(
		string key, string contentType, long contentLength, TimeSpan expiry, CancellationToken ct = default)
	{
		var request = new GetPreSignedUrlRequest
		{
			BucketName = _options.DefaultBucketName,
			Key = key,
			Verb = HttpVerb.PUT,
			Expires = DateTime.UtcNow.Add(expiry),
			ContentType = contentType,
		};

		var uploadUrl = RewriteOrigin(s3.GetPreSignedURL(request), _options.PublicEndpoint);
		var publicUrl = BuildPublicUrl(key);

		return Task.FromResult((uploadUrl, publicUrl));
	}

	public async Task DeleteAsync(string key, CancellationToken ct = default)
	{
		await s3.DeleteObjectAsync(_options.DefaultBucketName, key, ct);
	}

	public string? GetKeyFromPublicUrl(string publicUrl)
	{
		var prefix = $"{_options.SanitizedPublicEndpoint}/{_options.DefaultBucketName}/";

		return !publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				? null
				: WebUtility.UrlDecode(publicUrl[prefix.Length..]);
	}

	private static string RewriteOrigin(string url, string serviceUrl)
	{
		var generated = new Uri(url);
		var target = new Uri(serviceUrl);
		var rewritten = new UriBuilder(generated) { Scheme = target.Scheme, Host = target.Host, Port = target.Port };
		return rewritten.Uri.ToString();
	}

	private string BuildPublicUrl(string key)
	{
		var encodedKey = WebUtility.UrlEncode(key.TrimStart('/'));
		return $"{_options.SanitizedPublicEndpoint}/{_options.DefaultBucketName}/{encodedKey}";
	}
}
