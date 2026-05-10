using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/staff-invitations")]
[Authorize]
public sealed class StaffInvitationsController(
	AppDbContext db,
	StaffInvitationService invitationService,
	KeycloakAdminService keycloak,
	ILogger<StaffInvitationsController> logger) : ControllerBase
{
	public record InvitationDto(
		Guid Id,
		Guid StaffId,
		string StaffName,
		string Email,
		string Status,
		DateTimeOffset ExpiresAt,
		DateTimeOffset? AcceptedAt,
		DateTimeOffset CreatedAt);

	public record AcceptInvitationRequest(string Token, string KeycloakSubject);

	[HttpGet]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<List<InvitationDto>>> GetAll(CancellationToken ct)
	{
		var invitations = await db.StaffInvitations
								  .AsNoTracking()
								  .Include(i => i.Staff)
								  .OrderByDescending(i => i.CreatedAt)
								  .ToListAsync(ct);

		return Ok(invitations.Select(ToDto).ToList());
	}

	[HttpGet("by-staff/{staffId:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<List<InvitationDto>>> GetByStaff(Guid staffId, CancellationToken ct)
	{
		var invitations = await db.StaffInvitations
								  .AsNoTracking()
								  .Include(i => i.Staff)
								  .Where(i => i.StaffId == staffId)
								  .OrderByDescending(i => i.CreatedAt)
								  .ToListAsync(ct);

		return Ok(invitations.Select(ToDto).ToList());
	}

	[HttpPost("invite/{staffId:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<InvitationDto>> SendInvite(Guid staffId, CancellationToken ct)
	{
		var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == staffId, ct);
		if (staff is null)
		{
			return NotFound();
		}

		var currentUserSubject = User.GetKeycloakSubject();
		if (staff.KeycloakSubject != null && staff.KeycloakSubject == currentUserSubject)
		{
			return BadRequest(new ProblemDetails { Title = "Ugyldig handling", Detail = "Du kan ikke invitere dig selv.", Status = 400 });
		}

		if (string.IsNullOrWhiteSpace(staff.Email))
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["email"] = ["Medarbejderen har ingen e-mailadresse. Tilføj en e-mail og prøv igen."] }
			});
		}

		var emailAlreadyClaimed = await db.Staff
			.AnyAsync(s => s.Id != staffId && s.Email == staff.Email && s.KeycloakSubject != null, ct);
		if (emailAlreadyClaimed)
		{
			return Problem(
				title: "E-mailadresse allerede i brug",
				detail: "En anden medarbejder har allerede accepteret en invitation med denne e-mailadresse.",
				statusCode: 409);
		}

		try
		{
			var invitation = await invitationService.CreateAndSendAsync(staff, ct);
			var withStaff = await db.StaffInvitations
									.AsNoTracking()
									.Include(i => i.Staff)
									.FirstAsync(i => i.Id == invitation.Id, ct);

			return Ok(ToDto(withStaff));
		}
		catch (InvalidOperationException ex)
		{
			return Problem(
				title: "Konfigurationsfejl",
				detail: ex.Message,
				statusCode: 500);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send invitation email for staff {StaffId}", staffId);
			return Problem(
				title: "Kunne ikke sende invitation",
				detail: "Der opstod en fejl ved afsendelse af invitationsmail. Prøv igen.",
				statusCode: 502);
		}
	}

	[HttpPost("accept")]
	[Authorize]
	public async Task<ActionResult> Accept([FromBody] AcceptInvitationRequest req, CancellationToken ct)
	{
		// Extract authenticated subject from claims
		var keycloakSubject = User.GetKeycloakSubject();

		if (string.IsNullOrEmpty(keycloakSubject))
		{
			return Unauthorized(new ProblemDetails
			{
				Title = "Ikke autentificeret",
				Detail = "Brugeren er ikke autentificeret.",
				Status = 401
			});
		}

		var invitation = await invitationService.FindValidAsync(req.Token, ct);
		if (invitation is null)
		{
			return Problem(
				title: "Ugyldig eller udløbet invitation",
				detail: "Invitationslinket er ugyldigt eller udløbet.",
				statusCode: 400);
		}

		await invitationService.MarkAcceptedAsync(invitation, keycloakSubject, ct);

		if (invitation.Staff.IsAdmin)
		{
			try
			{
				await keycloak.SetAdminRoleAsync(keycloakSubject, grant: true, ct);
			}
			catch (KeycloakException ex)
			{
				return Problem(title: "Keycloak-synkronisering fejlede", detail: ex.Message, statusCode: 502);
			}
		}

		return NoContent();
	}

	[HttpGet("preview")]
	[AllowAnonymous]
	public async Task<ActionResult> PreviewInvitation([FromQuery] string token, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(token))
		{
			return BadRequest(new ProblemDetails
			{
				Title = "Token mangler",
				Detail = "Invitationstoken skal være angivet.",
				Status = 400
			});
		}

		var invitation = await invitationService.FindValidAsync(token, ct);
		if (invitation is null)
		{
			return Problem(
				title: "Ugyldig eller udløbet invitation",
				detail: "Invitationslinket er ugyldigt eller udløbet.",
				statusCode: 404);
		}

		var school = await db.Schools
							 .IgnoreQueryFilters()
							 .Where(s => s.Id == invitation.TenantId)
							 .Select(s => new { s.Name })
							 .FirstOrDefaultAsync(ct);

		return Ok(new
		{
			staffName = invitation.Staff.Name,
			email = invitation.Email,
			schoolName = school?.Name ?? "Skoleoverblikket",
			expiresAt = invitation.ExpiresAt,
		});
	}

	private static InvitationDto ToDto(StaffInvitation i) => new(
		i.Id,
		i.StaffId,
		i.Staff.Name,
		i.Email,
		i.Status.ToString(),
		i.ExpiresAt,
		i.AcceptedAt,
		i.CreatedAt);
}
