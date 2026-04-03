using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("skoleplanen_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public TestTenantContext TenantContext { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace DB with the Testcontainers instance
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace tenant context — tests control TenantId directly
            var tenantDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantContext));
            if (tenantDescriptor != null) services.Remove(tenantDescriptor);

            services.AddScoped<ITenantContext>(_ => TenantContext);

            // Replace JWT auth with a no-op test scheme so [Authorize] passes
            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
        });

        builder.ConfigureServices(services =>
        {
            // Run EF migrations on first boot
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
