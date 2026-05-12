using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/classes/{classId:guid}/permissions")]
[Authorize]
public sealed class ClassPermissionsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
	public record ClassPermissionDto(Guid StaffId, string StaffName, DateTimeOffset GrantedAt);
	public record GrantPermissionRequest(Guid StaffId);

	[HttpGet]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<List<ClassPermissionDto>>> GetAll(Guid classId, CancellationToken ct)
	{
		var classExists = await db.Classes.AnyAsync(c => c.Id == classId, ct);
		if (!classExists)
		{
			return NotFound();
		}

		var permissions = await db.ClassPermissions
			.AsNoTracking()
			.Where(p => p.ClassId == classId)
			.OrderBy(p => p.Staff.Name)
			.Select(p => new ClassPermissionDto(p.StaffId, p.Staff.Name, p.GrantedAt))
			.ToListAsync(ct);

		return Ok(permissions);
	}

	[HttpPost]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<ClassPermissionDto>> Grant(Guid classId, [FromBody] GrantPermissionRequest req, CancellationToken ct)
	{

		var classExists = await db.Classes.AnyAsync(c => c.Id == classId, ct);
		if (!classExists)
		{
			return NotFound();
		}

		var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == req.StaffId, ct);
		if (staff is null)
		{
			return NotFound();
		}

		var alreadyExists = await db.ClassPermissions
			.AnyAsync(p => p.ClassId == classId && p.StaffId == req.StaffId, ct);

		if (alreadyExists)
		{
			return Problem(
				title: "Adgang allerede tildelt",
				detail: "Denne medarbejder har allerede adgang til klassen.",
				statusCode: 409);
		}

		var permission = new ClassPermission
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = classId,
			StaffId = req.StaffId,
			GrantedAt = DateTimeOffset.UtcNow,
		};

		db.ClassPermissions.Add(permission);
		await db.SaveChangesAsync(ct);

		return CreatedAtAction(nameof(GetAll), new { classId },
			new ClassPermissionDto(permission.StaffId, staff.Name, permission.GrantedAt));
	}

	[HttpDelete("{staffId:guid}")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult> Revoke(Guid classId, Guid staffId, CancellationToken ct)
	{
		var permission = await db.ClassPermissions
			.FirstOrDefaultAsync(p => p.ClassId == classId && p.StaffId == staffId, ct);

		if (permission is null)
		{
			return NotFound();
		}

		db.ClassPermissions.Remove(permission);
		await db.SaveChangesAsync(ct);

		return NoContent();
	}
}
