using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Auth;

public sealed class EditClassRequirement : IAuthorizationRequirement { }

/// <summary>
/// Resource-based handler. Resource is the classId (Guid).
/// Logic:
///   1. Admins (IsAdmin = true) always succeed.
///   2. If no ClassPermission rows exist for the class → open, all authenticated staff succeed.
///   3. If rows exist and one matches this staff member → succeed.
///   4. Otherwise → deny.
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
			return;
		}

		if (staff.IsAdmin)
		{
			context.Succeed(requirement);
			return;
		}

		var anyPermissionsForClass = await db.ClassPermissions
			.AnyAsync(p => p.ClassId == classId);

		if (!anyPermissionsForClass)
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
