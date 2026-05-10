using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Skoleoverblikket.Api.Tenancy;

public sealed class MissingTenantClaimExceptionHandler(ILogger<MissingTenantClaimExceptionHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
	{
		if (ex is not MissingTenantClaimException && ex?.InnerException is not MissingTenantClaimException)
		{
			return false;
		}

		logger.LogWarning("JWT is missing the tenant_id claim. Ensure the Keycloak client has a user attribute mapper for tenant_id.");

		var problem = new ProblemDetails
		{
			Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
			Title = "Unauthorized",
			Status = 401,
			Detail = "Missing tenant claim in token.",
		};

		ctx.Response.StatusCode = 401;
		await ctx.Response.WriteAsJsonAsync(problem, ct);
		return true;
	}
}
