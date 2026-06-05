using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/kontakt")]
[Authorize]
public sealed class KontaktController(AppDbContext db) : ControllerBase
{
	public record KontaktParentDto(
		Guid Id,
		string Name,
		string? Phone,
		string? Address,
		string? PostalCode,
		string? City,
		string? AvatarUrl,
		IReadOnlyList<string> StudentNames);

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<KontaktParentDto>>> GetKontakt(CancellationToken ct)
	{
		var subject = User.GetKeycloakSubject();

		if (string.IsNullOrEmpty(subject))
		{
			return Unauthorized();
		}

		var isAdmin = User.IsInRole(Roles.Admin);
		var isParent = User.IsInRole(Roles.Parent);
		var isStaff = !isAdmin && !isParent;

		IQueryable<Models.Parent> query;

		if (isAdmin)
		{
			// Admin sees all parents, all fields, no consent filter
			query = db.Parents.Include(p => p.Students);
		}
		else if (isParent)
		{
			// Parent sees only co-class parents with ShareContactInfo=true
			var myStudentIds = db.Parents
				.Where(p => p.KeycloakSubject == subject)
				.SelectMany(p => p.Students.Select(s => s.Id));

			var myClassIds = db.Students
				.Where(s => myStudentIds.Contains(s.Id))
				.Select(s => s.ClassId);

			var coClassStudentIds = db.Students
				.Where(s => myClassIds.Contains(s.ClassId))
				.Select(s => s.Id);

			query = db.Parents
				.Include(p => p.Students)
				.Where(p => p.ShareContactInfo
					&& p.Students.Any(s => coClassStudentIds.Contains(s.Id)));
		}
		else if (isStaff)
		{
			// Staff sees all parents with ShareContactInfo=true
			query = db.Parents
				.Include(p => p.Students)
				.Where(p => p.ShareContactInfo);
		}
		else
		{
			return Forbid();
		}

		var parents = await query
			.AsNoTracking()
			.OrderBy(p => p.Name)
			.ToListAsync(ct);

		var dtos = parents.Select(p =>
		{
			var hideDetails = p.AdresseBeskyttet && !isAdmin;
			return new KontaktParentDto(
				p.Id,
				p.Name,
				hideDetails ? null : p.Phone,
				hideDetails ? null : p.Address,
				hideDetails ? null : p.PostalCode,
				hideDetails ? null : p.City,
				p.AvatarUrl,
				p.Students.Select(s => s.Name).OrderBy(n => n).ToList());
		}).ToList();

		return Ok(dtos);
	}
}
