using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Services;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/staff-invitations")]
[Authorize]
public sealed class StaffInvitationsController(
    AppDbContext db,
    StaffInvitationService invitationService) : ControllerBase
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
            return NotFound();

        if (string.IsNullOrWhiteSpace(staff.Email))
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["email"] = ["Medarbejderen har ingen e-mailadresse. Tilføj en e-mail og prøv igen."] }
            });

        try
        {
            var invitation = await invitationService.CreateAndSendAsync(staff, ct);
            var withStaff = await db.StaffInvitations
                .AsNoTracking()
                .Include(i => i.Staff)
                .FirstAsync(i => i.Id == invitation.Id, ct);
            return Ok(ToDto(withStaff));
        }
        catch (Exception)
        {
            return Problem(
                title: "Kunne ikke sende invitation",
                detail: "Der opstod en fejl ved afsendelse af invitationsmail. Prøv igen.",
                statusCode: 502);
        }
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<ActionResult> Accept([FromBody] AcceptInvitationRequest req, CancellationToken ct)
    {
        var invitation = await invitationService.FindValidAsync(req.Token, ct);
        if (invitation is null)
            return Problem(
                title: "Ugyldig eller udløbet invitation",
                detail: "Invitationslinket er ugyldigt eller udløbet.",
                statusCode: 400);

        await invitationService.MarkAcceptedAsync(invitation, req.KeycloakSubject, ct);
        return NoContent();
    }

    [HttpGet("preview/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult> PreviewInvitation(string token, CancellationToken ct)
    {
        var invitation = await invitationService.FindValidAsync(token, ct);
        if (invitation is null)
            return Problem(
                title: "Ugyldig eller udløbet invitation",
                detail: "Invitationslinket er ugyldigt eller udløbet.",
                statusCode: 404);

        var school = await db.Schools
            .IgnoreQueryFilters()
            .Where(s => s.Id == invitation.TenantId)
            .Select(s => new { s.Name, s.Slug })
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            staffName = invitation.Staff.Name,
            email = invitation.Email,
            schoolName = school?.Name ?? "Skoleplanen",
            schoolSlug = school?.Slug,
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
