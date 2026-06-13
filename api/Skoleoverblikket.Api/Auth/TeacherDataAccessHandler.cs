using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Auth;

public sealed class TeacherDataAccessRequirement : IAuthorizationRequirement { }

public sealed class TeacherDataAccessHandler(AppDbContext db) : AuthorizationHandler<TeacherDataAccessRequirement>
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

		var member = await db.BoardMembers
			.IgnoreQueryFilters()
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.KeycloakSubject == sub);

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
