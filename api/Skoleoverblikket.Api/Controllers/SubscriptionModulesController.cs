using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

/// <summary>
/// Read-only module access endpoint — available to all authenticated users (admins, staff, parents).
/// </summary>
[ApiController]
[Route("api/v1/modules")]
[Authorize]
public sealed class SubscriptionModulesController(
	SubscriptionService subscriptionService,
	ITenantContext tenantContext) : ControllerBase
{
	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<string>>> GetActiveModules(CancellationToken ct)
	{
		var modules = await subscriptionService.GetActiveModulesAsync(tenantContext.TenantId, ct);
		return Ok(modules);
	}
}
