using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Controllers;

/// <summary>
/// Superadmin-only endpoints for managing tenant module access without Stripe.
/// Only users with the 'superadmin' Keycloak realm role can call these.
/// </summary>
[ApiController]
[Route("api/v1/admin/tenants/{schoolId:guid}/modules")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class AdminController(SubscriptionService subscriptionService) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> GrantModule(Guid schoolId, [FromBody] ModuleOverrideRequest request, CancellationToken ct)
	{
		try
		{
			await subscriptionService.GrantModuleOverrideAsync(schoolId, request.Module, ct);
			return NoContent();
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
		{
			return NotFound(new { detail = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			return Problem(title: "Modul override fejlede", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
		}
	}

	[HttpDelete("{module}")]
	public async Task<IActionResult> RevokeModule(Guid schoolId, SubscriptionModule module, CancellationToken ct)
	{
		try
		{
			await subscriptionService.RemoveModuleAsync(schoolId, module, ct);
			return NoContent();
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
		{
			return NotFound(new { detail = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			return Problem(title: "Modul kunne ikke fjernes", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
		}
	}

	public record ModuleOverrideRequest(SubscriptionModule Module);
}
