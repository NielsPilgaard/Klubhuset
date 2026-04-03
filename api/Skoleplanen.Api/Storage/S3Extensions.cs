using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;

namespace Skoleplanen.Api.Storage;

public static class S3Extensions
{
    public static IServiceCollection AddObjectStorage(this IServiceCollection services)
    {
        services.AddOptions<S3Options>()
                .BindConfiguration(S3Options.SectionName)
                .ValidateDataAnnotations();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = opts.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = RegionEndpoint.EUWest1.SystemName,
            };

            return new AmazonS3Client(new BasicAWSCredentials(opts.AccessKey, opts.SecretKey), config);
        });

        services.AddScoped<IObjectStorage, S3ObjectStorage>();

        return services;
    }

    /// <summary>Ensures the configured S3 bucket exists. Idempotent — no-op if already present.</summary>
    public static async Task EnsureS3BucketAsync(this IServiceProvider services)
    {
        var s3 = services.GetRequiredService<IAmazonS3>();
        var opts = services.GetRequiredService<IOptions<S3Options>>().Value;

        if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, opts.BucketName))
        {
            await s3.PutBucketAsync(opts.BucketName);
        }
    }
}
