using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Auth;

public sealed class TeacherDataAccessRequirement : IAuthorizationRequirement { }

public sealed class TeacherDataAccessHandler(AppDbContext db, ITenantContext tenant) : AuthorizationHandler<TeacherDataAccessRequirement>
{
	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		TeacherDataAccessRequirement requirement)
	{
		if (context.User.IsInRole(Roles.Admin))
		{
			context.Succeed(requirement);
			return;
		}

		if (!context.User.IsInRole(Roles.Board))
		{
			context.Fail();
			return;
		}

		var sub = context.User.FindFirst("sub")?.Value;
		if (string.IsNullOrEmpty(sub))
		{
			context.Fail();
			return;
		}

		// Use tenant-scoped query (no IgnoreQueryFilters) to prevent cross-tenant access
		var member = await db.BoardMembers
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.KeycloakSubject == sub && m.TenantId == tenant.TenantId);

		if (member?.CanAccessTeacherData == true)
		{
			context.Succeed(requirement);
		}
		else
		{
			context.Fail();
		}
	}
}
