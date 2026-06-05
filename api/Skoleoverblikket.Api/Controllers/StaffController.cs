using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;
using ZiggyCreatures.Caching.Fusion;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize]
public sealed class StaffController(AppDbContext db, ITenantContext tenant, KeycloakAdminService keycloak, ILogger<StaffController> logger, IFusionCache cache) : ControllerBase
{
	public record StaffDto(Guid Id, string Name, string? Email, string? Phone, StaffRole Role, bool IsAdmin, string? KeycloakSubject);
	public record UpsertStaffRequest(string Name, string? Email, string? Phone, StaffRole Role, bool IsAdmin = false);
	public record PatchAdminPermissionRequest(bool IsAdmin);

	[HttpGet]
	public async Task<ActionResult<List<StaffDto>>> GetAll(CancellationToken cancellationToken)
	{
		var staff = await db.Staff
			.AsNoTracking()
			.OrderBy(s => s.Name)
			.Select(s => new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject))
			.ToListAsync(cancellationToken);
		return Ok(staff);
	}

	[HttpGet("me")]
	public async Task<ActionResult<StaffDto>> GetMe(CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		if (string.IsNullOrWhiteSpace(subject))
		{
			return Unauthorized();
		}

		var staff = await db.Staff
			.AsNoTracking()
			.Where(s => s.KeycloakSubject == subject)
			.Select(s => new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject))
			.FirstOrDefaultAsync(cancellationToken);

		return staff is null
			? NotFound()
			: Ok(staff);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<StaffDto>> GetById(Guid id, CancellationToken cancellationToken)
	{
		var staff = await db.Staff
							.AsNoTracking()
							.Where(s => s.Id == id)
							.Select(s => new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject))
							.FirstOrDefaultAsync(cancellationToken);

		return staff is null
				   ? NotFound()
				   : Ok(staff);
	}

	[HttpPost]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<StaffDto>> Create([FromBody] UpsertStaffRequest req, CancellationToken cancellationToken)
	{
		if (!string.IsNullOrWhiteSpace(req.Email))
		{
			var emailTaken = await db.Staff.AnyAsync(s => s.Email == req.Email, cancellationToken);
			if (emailTaken)
			{
				return ValidationProblem(new ValidationProblemDetails
				{
					Errors = { ["email"] = ["En medarbejder med denne e-mailadresse findes allerede."] }
				});
			}
		}

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
		await db.SaveChangesAsync(cancellationToken);
		await cache.RemoveAsync(SchoolsController.OnboardingCacheKey(tenant.TenantId), token: cancellationToken);
		return CreatedAtAction(nameof(GetById), new { id = s.Id },
			new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<StaffDto>> Update(Guid id, [FromBody] UpsertStaffRequest req, CancellationToken cancellationToken)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
		if (s is null)
		{
			return NotFound();
		}

		if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != s.Email)
		{
			var emailTaken = await db.Staff.AnyAsync(other => other.Id != id && other.Email == req.Email, cancellationToken);
			if (emailTaken)
			{
				return ValidationProblem(new ValidationProblemDetails
				{
					Errors = { ["email"] = ["En medarbejder med denne e-mailadresse findes allerede."] }
				});
			}
		}

		var isAdminChanged = s.IsAdmin != req.IsAdmin;

		s.Name = req.Name;
		s.Email = req.Email;
		s.Phone = req.Phone;
		s.Role = req.Role;
		s.IsAdmin = req.IsAdmin;

		if (isAdminChanged && s.KeycloakSubject is not null)
		{
			var validationResult = await ValidateAdminChangeAsync(id, req.IsAdmin, cancellationToken);
			if (validationResult is not null)
			{
				return validationResult;
			}

			await db.SaveChangesAsync(cancellationToken);
			try
			{
				await keycloak.SetAdminRoleAsync(s.KeycloakSubject, req.IsAdmin, cancellationToken);
			}
			catch (KeycloakException ex)
			{
				s.IsAdmin = !req.IsAdmin;
				await db.SaveChangesAsync(cancellationToken);
				return Problem(title: "Keycloak-synkronisering fejlede", detail: ex.Message, statusCode: 502);
			}
		}
		else
		{
			await db.SaveChangesAsync(cancellationToken);
		}

		return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
	}

	[HttpPatch("{id:guid}/admin-permission")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<StaffDto>> PatchAdminPermission(Guid id, [FromBody] PatchAdminPermissionRequest req, CancellationToken cancellationToken)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
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

		var validationResult = await ValidateAdminChangeAsync(id, req.IsAdmin, cancellationToken);
		if (validationResult is not null)
		{
			return validationResult;
		}

		s.IsAdmin = req.IsAdmin;
		await db.SaveChangesAsync(cancellationToken);

		try
		{
			await keycloak.SetAdminRoleAsync(s.KeycloakSubject, req.IsAdmin, cancellationToken);
		}
		catch (KeycloakException ex)
		{
			s.IsAdmin = !req.IsAdmin;
			await db.SaveChangesAsync(cancellationToken);
			return Problem(title: "Keycloak-synkronisering fejlede", detail: ex.Message, statusCode: 502);
		}

		return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role, s.IsAdmin, s.KeycloakSubject));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
		if (s is null)
		{
			return NotFound();
		}

		var currentSubject = User.GetKeycloakSubject();
		if (!string.IsNullOrWhiteSpace(s.KeycloakSubject) && s.KeycloakSubject == currentSubject)
		{
			return Problem(
				detail: "Du kan ikke slette din egen konto.",
				statusCode: StatusCodes.Status403Forbidden);
		}

		var hasSlots = await db.SchemaSlots.AnyAsync(sl => sl.TeacherId == id || sl.AideId == id, cancellationToken);
		if (hasSlots)
		{
			return Problem(
				detail: "Medarbejderen er tildelt en eller flere lektioner og kan ikke slettes. Fjern medarbejderen fra alle lektioner først.",
				statusCode: StatusCodes.Status409Conflict);
		}

		if (!string.IsNullOrWhiteSpace(s.KeycloakSubject))
		{
			try
			{
				await keycloak.DeleteStaffUserAsync(s.KeycloakSubject, cancellationToken);
			}
			catch (KeycloakException ex)
			{
				logger.LogWarning(ex, "Could not delete Keycloak account for staff {StaffId}; DB record will still be removed", id);
			}
		}

		db.Staff.Remove(s);
		await db.SaveChangesAsync(cancellationToken);
		await cache.RemoveAsync(SchoolsController.OnboardingCacheKey(tenant.TenantId), token: cancellationToken);
		return NoContent();
	}

	private async Task<ActionResult?> ValidateAdminChangeAsync(Guid staffId, bool newIsAdmin, CancellationToken cancellationToken)
	{
		var currentUserId = User.GetKeycloakSubject();

		var staff = await db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);
		if (staff?.KeycloakSubject is not null && staff.KeycloakSubject == currentUserId && !newIsAdmin)
		{
			return Problem(
				title: "Ikke tilladt",
				detail: "Du kan ikke fjerne din egen administratoradgang.",
				statusCode: 409);
		}

		if (!newIsAdmin)
		{
			var adminCount = await db.Staff.CountAsync(s => s.IsAdmin, cancellationToken);
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
