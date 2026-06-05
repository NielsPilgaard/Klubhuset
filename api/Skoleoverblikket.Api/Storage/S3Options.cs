using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Storage;

public sealed class S3Options
{
	public const string SectionName = "ObjectStorage";

	[Required(AllowEmptyStrings = false)]
	public required string ServiceUrl { get; init; }

	[Required(AllowEmptyStrings = false)]
	public required string AccessKey { get; init; }

	[Required(AllowEmptyStrings = false)]
	public required string SecretKey { get; init; }

	[Required(AllowEmptyStrings = false)]
	public required string DefaultBucketName { get; init; }

	[Required(AllowEmptyStrings = false)]
	public required string PublicEndpoint { get; init; }

	[Required(AllowEmptyStrings = false)]
	public required string PresignedUploadSigningKey { get; init; }

	public string SanitizedPublicEndpoint => PublicEndpoint.TrimEnd('/');
}
