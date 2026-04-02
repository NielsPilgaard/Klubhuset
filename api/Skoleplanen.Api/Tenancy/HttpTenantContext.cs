using System.Security.Claims;

namespace Skoleplanen.Api.Tenancy;

/// <summary>
/// Resolves the current tenant from the authenticated JWT claim.
/// The tenant_id claim is set by Keycloak after the user's school is looked up at login.
/// Never trust a URL slug — always resolve to a TenantId at the middleware boundary.
/// </summary>
public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var claim = accessor.HttpContext?.User.FindFirstValue("tenant_id")
                ?? throw new InvalidOperationException("tenant_id claim not found. Endpoint must be authenticated.");

            return Guid.Parse(claim);
        }
    }
}
