using System.Security.Claims;

namespace Skoleoverblikket.Api.Auth;

public static class ClaimsPrincipalExtensions
{
	/// Keycloak maps the subject to "sub"; .NET maps NameIdentifier to the same claim.
	/// Try both to handle different middleware configurations.
	public static string? GetKeycloakSubject(this ClaimsPrincipal user) =>
		user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
