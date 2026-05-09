using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize]
public sealed class StaffController(AppDbContext db, ITenantContext tenant, KeycloakAdminService keycloak, ILogger<StaffController> logger) : ControllerBase
{
	public record StaffDto(Guid Id, string Name, string? Email, string? Phone, StaffRole Role, bool IsAdmin, string? KeycloakSubject);
	public record UpsertStaffRequest(string Name, string? Email, string? Phone, StaffRole Role, bool IsAdmin = false);
	public record PatchAdminPermissionRequest(bool IsAdmin);

	[HttpGet]
	public async Task<ActionResult<List<StaffDto>>> GetAll(CancellationToken ct)
	{
		var staff = await db.Staff
			.AsNoTracking()
			.OrderBy(s => s.Name)
			.Select(s => new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject))
			.ToListAsync(ct);
		return Ok(staff);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<StaffDto>> GetById(Guid id, CancellationToken ct)
	{
		var staff = await db.Staff
							.AsNoTracking()
							.FirstOrDefaultAsync(s => s.Id == id, ct);

		return staff is null
				   ? NotFound()
				   : Ok(new StaffDto(staff.Id, staff.Name, staff.Email, staff.Phone, staff.Role, staff.IsAdmin, staff.KeycloakSubject));
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<StaffDto>> Create([FromBody] UpsertStaffRequest req, CancellationToken ct)
	{
		var s = new Staff
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Email = req.Email,
			Phone = req.Phone,
			Role = req.Role,
			IsAdmin = req.IsAdmin,
		};
		db.Staff.Add(s);
		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById), new { id = s.Id },
			new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<StaffDto>> Update(Guid id, [FromBody] UpsertStaffRequest req, CancellationToken ct)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (s is null)
		{
			return NotFound();
		}

		var isAdminChanged = s.IsAdmin != req.IsAdmin;

		s.Name = req.Name;
		s.Email = req.Email;
		s.Phone = req.Phone;
		s.Role = req.Role;
		s.IsAdmin = req.IsAdmin;

		if (isAdminChanged && s.KeycloakSubject is not null)
		{
			var validationResult = await ValidateAdminChangeAsync(id, req.IsAdmin, ct);
			if (validationResult is not null)
			{
				return validationResult;
			}

			await db.SaveChangesAsync(ct);
			try
			{
				await keycloak.SetAdminRoleAsync(s.KeycloakSubject, req.IsAdmin, ct);
			}
			catch (KeycloakException ex)
			{
				return Problem(title: "Keycloak-synkronisering fejlede", detail: ex.Message, statusCode: 502);
			}
		}
		else
		{
			await db.SaveChangesAsync(ct);
		}

		return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
	}

	[HttpPatch("{id:guid}/admin-permission")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<StaffDto>> PatchAdminPermission(Guid id, [FromBody] PatchAdminPermissionRequest req, CancellationToken ct)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (s is null)
		{
			return NotFound();
		}

		if (s.KeycloakSubject is null)
		{
			return Problem(
				title: "Invitation ikke accepteret",
				detail: "Medarbejderen skal acceptere invitationen, før administratoradgang kan ændres.",
				statusCode: 409);
		}

		if (s.IsAdmin == req.IsAdmin)
		{
			return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
		}

		var validationResult = await ValidateAdminChangeAsync(id, req.IsAdmin, ct);
		if (validationResult is not null)
		{
			return validationResult;
		}

		s.IsAdmin = req.IsAdmin;
		await db.SaveChangesAsync(ct);

		try
		{
			await keycloak.SetAdminRoleAsync(s.KeycloakSubject, req.IsAdmin, ct);
		}
		catch (KeycloakException ex)
		{
			s.IsAdmin = !req.IsAdmin;
			await db.SaveChangesAsync(ct);
			return Problem(title: "Keycloak-synkronisering fejlede", detail: ex.Message, statusCode: 502);
		}

		return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (s is null)
		{
			return NotFound();
		}

		if (!string.IsNullOrWhiteSpace(s.KeycloakSubject))
		{
			try
			{
				await keycloak.DeleteStaffUserAsync(s.KeycloakSubject, ct);
			}
			catch (KeycloakException ex)
			{
				logger.LogWarning(ex, "Could not delete Keycloak account for staff {StaffId}; DB record will still be removed", id);
			}
		}

		db.Staff.Remove(s);
		await db.SaveChangesAsync(ct);
		return NoContent();
	}

	private async Task<ActionResult?> ValidateAdminChangeAsync(Guid staffId, bool newIsAdmin, CancellationToken ct)
	{
		var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
						 ?? User.FindFirst("sub")?.Value;

		var staff = await db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffId, ct);
		if (staff?.KeycloakSubject is not null && staff.KeycloakSubject == currentUserId && !newIsAdmin)
		{
			return Problem(
				title: "Ikke tilladt",
				detail: "Du kan ikke fjerne din egen administratoradgang.",
				statusCode: 409);
		}

		if (!newIsAdmin)
		{
			var adminCount = await db.Staff.CountAsync(s => s.IsAdmin, ct);
			if (adminCount <= 1)
			{
				return Problem(
					title: "Ikke tilladt",
					detail: "Der skal altid være mindst én administrator. Tildel administratoradgang til en anden medarbejder først.",
					statusCode: 409);
			}
		}

		return null;
	}
}
