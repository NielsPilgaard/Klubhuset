using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Fixed-tenant context used in integration tests. Replaces the HTTP-based
/// <see cref="HttpTenantContext"/> so tests don't need a real JWT.
/// </summary>
public sealed class TestTenantContext : ITenantContext
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid TenantId { get; set; } = DefaultTenantId;
}
