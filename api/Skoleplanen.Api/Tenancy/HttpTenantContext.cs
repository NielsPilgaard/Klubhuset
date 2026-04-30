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
                ?? throw new MissingTenantClaimException();

            return Guid.Parse(claim);
        }
    }
}

/// <summary>
/// Thrown when a request reaches tenant-scoped code without a tenant_id claim.
/// This happens when a user's JWT was issued without the Keycloak attribute mapper
/// configured, or when an unauthenticated request bypasses auth middleware.
/// </summary>
public sealed class MissingTenantClaimException()
    : Exception("tenant_id claim not present in JWT. Check Keycloak client mapper configuration.");
