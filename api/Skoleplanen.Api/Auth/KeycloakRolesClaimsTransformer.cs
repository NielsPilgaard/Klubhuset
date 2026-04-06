using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Skoleplanen.Api.Auth;

/// <summary>
/// Keycloak puts realm roles in realm_access.roles (a nested JSON object),
/// but ASP.NET Core's [Authorize(Roles = "...")] looks for flat role claims.
/// This transformer flattens them into standard ClaimTypes.Role claims.
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
            if (!doc.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                return Task.FromResult(principal);
            }

            foreach (var role in rolesElement.EnumerateArray())
            {
                var roleName = role.GetString();
                if (string.IsNullOrEmpty(roleName))
                {
                    continue;
                }

                if (!principal.HasClaim(ClaimTypes.Role, roleName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }
        catch (JsonException)
        {
            // Malformed claim — skip silently
        }

        return Task.FromResult(principal);
    }
}
