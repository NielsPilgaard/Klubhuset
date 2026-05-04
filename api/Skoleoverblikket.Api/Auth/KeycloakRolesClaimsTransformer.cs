using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Skoleoverblikket.Api.Auth;

/// <summary>
/// Keycloak emits realm roles as a JSON-valued claim: realm_access = {"roles":["admin",...]}
/// (ValueType = "JSON"). ASP.NET Core's [Authorize(Roles = "...")] looks for ClaimTypes.Role,
/// so we parse the JSON and add a flat role claim per entry.
/// </summary>
public sealed class KeycloakRolesClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var realmAccessClaim = principal.FindFirst("realm_access");
        if (realmAccessClaim is null)
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim.Value);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesArray))
            {
                return Task.FromResult(principal);
            }

            foreach (var role in rolesArray.EnumerateArray())
            {
                var roleName = role.GetString();
                if (!string.IsNullOrEmpty(roleName) && !principal.HasClaim(ClaimTypes.Role, roleName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }
        catch (JsonException) { }

        return Task.FromResult(principal);
    }
}
