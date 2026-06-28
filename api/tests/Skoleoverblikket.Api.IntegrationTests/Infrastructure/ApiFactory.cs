using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Storage;
using Skoleoverblikket.Api.Tenancy;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;
using TUnit.AspNetCore;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up a real PostgreSQL container and a LocalStack S3 container (via Testcontainers)
/// alongside the full ASP.NET Core pipeline. Auth is replaced by a configurable
/// <see cref="TestTenantContext"/> and a simple test-only JWT scheme so
/// tests can control which tenant they're operating as.
/// </summary>
public sealed class ApiFactory : TestWebApplicationFactory<Program>
{
	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
		.WithDatabase("skoleoverblikket_test")
		.WithUsername("test")
		.WithPassword("test")
		.Build();

	private readonly LocalStackContainer _localStack = new LocalStackBuilder("localstack/localstack:4").Build();

	public TestTenantContext TenantContext { get; } = new();

	public async Task StartAsync()
	{
		await Task.WhenAll(_postgres.StartAsync(), _localStack.StartAsync());

		await using var scope = Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();

		var s3 = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
		var opts = scope.ServiceProvider.GetRequiredService<IOptions<S3Options>>().Value;
		var exists = await AmazonS3Util.DoesS3BucketExistV2Async(s3, opts.DefaultBucketName);
		if (!exists)
		{
			await s3.PutBucketAsync(new PutBucketRequest { BucketName = opts.DefaultBucketName });
		}
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureAppConfiguration(config =>
		{
			var localStackUrl = _localStack.GetConnectionString();
			config.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ObjectStorage:ServiceUrl"] = localStackUrl,
				["ObjectStorage:AccessKey"] = "test",
				["ObjectStorage:SecretKey"] = "test",
				["ObjectStorage:DefaultBucketName"] = "skoleoverblikket-test",
				["ObjectStorage:PublicEndpoint"] = localStackUrl,
				["ObjectStorage:PresignedUploadSigningKey"] = "test-signing-key",
			});
		});

		builder.ConfigureServices(services =>
		{
			// Replace DB with the Testcontainers instance
			services.RemoveAll<DbContextOptions<AppDbContext>>();
			services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(_postgres.GetConnectionString()));

			// Replace tenant context — tests control TenantId directly
			services.RemoveAll<ITenantContext>();
			services.AddScoped<ITenantContext>(_ => TenantContext);

			// Replace JWT auth with a no-op test scheme so [Authorize] passes
			services.AddAuthentication(TestAuthHandler.SchemeName)
					.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
						TestAuthHandler.SchemeName, _ => { });

			// Replace notification service with a no-op so tests don't need an SMTP server
			services.RemoveAll<INotificationService>();
			services.AddScoped<INotificationService, NoOpNotificationService>();

			// Point S3 client and S3Options at LocalStack
			var localStackUrl = _localStack.GetConnectionString();
			services.RemoveAll<IAmazonS3>();
			services.AddSingleton<IAmazonS3>(_ =>
			{
				var config = new AmazonS3Config { ServiceURL = localStackUrl, ForcePathStyle = true };
				return new AmazonS3Client(new BasicAWSCredentials("test", "test"), config);
			});
		});
	}

	public async Task StopAsync()
	{
		await DisposeAsync();
		await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _localStack.DisposeAsync().AsTask());
	}
}
