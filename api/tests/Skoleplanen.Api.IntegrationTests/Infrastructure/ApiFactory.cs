using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Tenancy;
using Testcontainers.PostgreSql;

namespace Skoleplanen.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up a real PostgreSQL container (via Testcontainers) and the full
/// ASP.NET Core pipeline. Auth is replaced by a configurable
/// <see cref="TestTenantContext"/> and a simple test-only JWT scheme so
/// tests can control which tenant they're operating as.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
		.WithDatabase("skoleplanen_test")
		.WithUsername("test")
		.WithPassword("test")
		.Build();

	public TestTenantContext TenantContext { get; } = new();

	public async Task StartAsync()
	{
		await _postgres.StartAsync();
		await using var scope = Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

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
		});
	}

	public async Task StopAsync()
	{
		await DisposeAsync();
		await _postgres.DisposeAsync();
	}
}
