using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Auth;

public sealed class GroupMessageRequirement : IAuthorizationRequirement { }

/// <summary>
/// Resource-based handler. Resource is SendGroupMessageRequest (carries Audience + ClassId).
/// Admin → always succeed.
/// Non-admin staff → succeed for all audiences.
/// Parent → succeed only for ClassParents where ClassId is one of their children's classes.
/// </summary>
public sealed class GroupMessageAuthorizationHandler(AppDbContext db, ITenantContext tenant)
	: AuthorizationHandler<GroupMessageRequirement, GroupMessageRequest>
{
	protected override async Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		GroupMessageRequirement requirement,
		GroupMessageRequest resource)
	{
		var subject = context.User.GetKeycloakSubject();
		if (string.IsNullOrEmpty(subject))
		{
			return;
		}

		if (context.User.IsInRole(Roles.Admin))
		{
			var adminStaff = await db.Staff
				.AsNoTracking()
				.Where(s => s.KeycloakSubject == subject)
				.Select(s => new { s.IsAdmin })
				.FirstOrDefaultAsync();

			if (adminStaff?.IsAdmin == true || adminStaff is null)
			{
				context.Succeed(requirement);
				return;
			}
		}

		if (!context.User.IsInRole(Roles.Parent))
		{
			// Non-parent staff: all audiences allowed
			context.Succeed(requirement);
			return;
		}

		// Parent: only ClassParents allowed, and only for their children's classes
		if (resource.Audience != BroadcastAudience.ClassParents || resource.ClassId is null)
		{
			return;
		}

		var classIds = await db.Parents
			.AsNoTracking()
			.Where(p => p.KeycloakSubject == subject && p.TenantId == tenant.TenantId)
			.SelectMany(p => p.Students)
			.Select(s => s.ClassId)
			.Distinct()
			.ToListAsync();

		if (classIds.Contains(resource.ClassId.Value))
		{
			context.Succeed(requirement);
		}
	}
}

public sealed record GroupMessageRequest(BroadcastAudience Audience, Guid? ClassId, StaffRole? StaffRole);
