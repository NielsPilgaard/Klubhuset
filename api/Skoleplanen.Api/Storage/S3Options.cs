using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api.Storage;

public sealed class S3Options
{
    public const string SectionName = "ObjectStorage";

    [Required] public required string ServiceUrl { get; init; }
    [Required] public required string AccessKey { get; init; }
    [Required] public required string SecretKey { get; init; }
    [Required] public required string BucketName { get; init; }
    [Required] public required string PublicEndpoint { get; init; }
}
