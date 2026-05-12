using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Auth;

public sealed class EditClassRequirement : IAuthorizationRequirement { }

/// <summary>
/// Resource-based handler. Resource is the classId (Guid).
/// Logic:
///   1. No staff row but admin role claim → full access (superuser not enrolled in school).
///   2. Staff row has IsAdmin = true → full access (guards against Keycloak/DB desync).
///   3. No ClassPermission rows exist in the tenant → open, all authenticated staff succeed.
///   4. Permission rows exist and one matches this staff member → succeed.
///   5. Otherwise → deny.
/// </summary>
public sealed class EditClassAuthorizationHandler(AppDbContext db, ITenantContext tenant)
	: AuthorizationHandler<EditClassRequirement, Guid>
{
	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		EditClassRequirement requirement,
		Guid classId)
	{
		var subject = context.User.GetKeycloakSubject();
		if (string.IsNullOrEmpty(subject))
		{
			return;
		}

		var staff = await db.Staff
			.AsNoTracking()
			.Where(s => s.KeycloakSubject == subject && s.TenantId == tenant.TenantId)
			.Select(s => new { s.Id, s.IsAdmin })
			.FirstOrDefaultAsync();

		if (staff is null)
		{
			if (context.User.IsInRole(Roles.Admin))
			{
				context.Succeed(requirement);
			}

			return;
		}

		if (staff.IsAdmin)
		{
			context.Succeed(requirement);
			return;
		}

		var anyPermissionsExist = await db.ClassPermissions.AnyAsync();

		if (!anyPermissionsExist)
		{
			context.Succeed(requirement);
			return;
		}

		var hasPermission = await db.ClassPermissions
			.AnyAsync(p => p.ClassId == classId && p.StaffId == staff.Id);

		if (hasPermission)
		{
			context.Succeed(requirement);
		}
	}
}
