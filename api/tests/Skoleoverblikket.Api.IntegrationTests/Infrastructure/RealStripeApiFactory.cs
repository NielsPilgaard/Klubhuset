using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Services;
using Stripe;
using Stripe.TestHelpers;
using Testcontainers.PostgreSql;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Unlike <see cref="ApiFactory"/>, does NOT redirect Stripe calls to stripe-mock — it uses the
/// real Stripe test-mode API (key + price IDs read from configuration, same as local Aspire dev)
/// so tests can exercise real billing-cycle behavior (proration, invoice timing) via Stripe test
/// clocks. stripe-mock is a stateless fixture server and cannot simulate that.
///
/// Gated by the STRIPE_LIVE_TEST env var (see <see cref="IsAvailable"/>) — skipped by default so
/// `dotnet test` stays offline/fast. Requires Stripe:SecretKey and Stripe:BasePriceId* to be a
/// real Stripe test-mode key/prices — read from appsettings.Development.json via UseEnvironment
/// below, same source Aspire dev uses. No separate config needed.
/// </summary>
public sealed class RealStripeApiFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
	public static bool IsAvailable => Environment.GetEnvironmentVariable("STRIPE_LIVE_TEST") == "1";

	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
		.WithDatabase("skoleoverblikket_test")
		.WithUsername("test")
		.WithPassword("test")
		.Build();

	public async Task InitializeAsync()
	{
		await _postgres.StartAsync();

		await using var scope = Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Development");

		builder.ConfigureServices(services =>
		{
			services.RemoveAll<DbContextOptions<AppDbContext>>();
			services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(_postgres.GetConnectionString()));

			services.RemoveAll<INotificationService>();
			services.AddScoped<INotificationService, NoOpNotificationService>();

			// Same header-driven test auth as ApiFactory — X-Test-TenantId / X-Test-Roles / X-Test-Subject.
			services.AddAuthentication(TestAuthHandler.SchemeName)
					.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
						TestAuthHandler.SchemeName, _ => { });

			// IStripeClient intentionally left as the default real-Stripe registration from
			// StripeExtensions.AddStripe — no ApiBase override, unlike ApiFactory.

			// Test-only Stripe services not needed by production code (StripeExtensions.AddStripe
			// only registers what SubscriptionService uses).
			services.AddSingleton<TestClockService>();
			services.AddSingleton<InvoiceService>();
			services.AddSingleton<PriceService>();
		});
	}

	public override async ValueTask DisposeAsync()
	{
		await base.DisposeAsync();
		await _postgres.DisposeAsync().AsTask();
	}
}
