using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Storage;

namespace Skoleoverblikket.Api.Data;

public static class DatabaseExtensions
{
	public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddDbContext<AppDbContext>(options =>
			options.UseNpgsql(configuration.GetConnectionString("skoleoverblikket-db")));

		return services;
	}

	public static async Task MigrateAndSeedAsync(this WebApplication app)
	{
		var isOpenApiGeneration = string.Equals(
			Environment.GetEnvironmentVariable("OPENAPI_GENERATE"), "true",
			StringComparison.OrdinalIgnoreCase);

		if (!app.Environment.IsProduction() && !isOpenApiGeneration)
		{
			await using var scope = app.Services.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			await db.Database.MigrateAsync();
		}

		if (!app.Environment.IsEnvironment("Testing"))
		{
			if (!string.IsNullOrEmpty(app.Configuration.GetConnectionString("skoleoverblikket-db")))
			{
				_ = Task.Run(() => app.Services.SeedAsync());
			}

			if (!string.IsNullOrEmpty(app.Configuration["ObjectStorage:ServiceUrl"]))
			{
				_ = Task.Run(() => app.Services.EnsureS3BucketAsync());
			}
		}
	}
}
