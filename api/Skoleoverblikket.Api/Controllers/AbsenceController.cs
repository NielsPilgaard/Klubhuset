using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/absence")]
[Authorize]
public sealed class AbsenceController(AppDbContext db, INotificationService notifications, IAuthorizationService authorization) : ControllerBase
{
	public record ReportAbsenceRequest(Guid StudentId, DateOnly Date, DateOnly? EndDate, string? Reason);
	public record AbsenceReportDto(
		Guid Id, Guid StudentId, string StudentName,
		DateOnly Date, DateOnly? EndDate, string? Reason,
		AbsenceStatus Status, DateTimeOffset CreatedAt);

	[HttpPost]
	[Authorize(Roles = Roles.Parent)]
	public async Task<IActionResult> ReportAbsence(
		[FromBody] ReportAbsenceRequest req, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var parent = await db.Parents
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, cancellationToken);

		if (parent is null)
		{
			return Forbid();
		}

		var parentOwnsStudent = await db.Parents
			.AnyAsync(p => p.KeycloakSubject == subject && p.Students.Any(s => s.Id == req.StudentId), cancellationToken);

		if (!parentOwnsStudent)
		{
			return Forbid();
		}

		var report = new AbsenceReport
		{
			TenantId = parent.TenantId,
			StudentId = req.StudentId,
			ReportedByParentId = parent.Id,
			Date = req.Date,
			EndDate = req.EndDate,
			Reason = req.Reason,
		};

		db.AbsenceReports.Add(report);
		await db.SaveChangesAsync(cancellationToken);

		return CreatedAtAction(nameof(GetMine), new { }, null);
	}

	[HttpGet("mine")]
	[Authorize(Roles = Roles.Parent)]
	public async Task<ActionResult<IReadOnlyList<AbsenceReportDto>>> GetMine(CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var parent = await db.Parents.AsNoTracking()
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, cancellationToken);

		if (parent is null)
		{
			return Ok(Array.Empty<AbsenceReportDto>());
		}

		var reports = await db.AbsenceReports
			.AsNoTracking()
			.Include(a => a.Student)
			.Where(a => a.ReportedByParentId == parent.Id)
			.OrderByDescending(a => a.Date)
			.Select(a => new AbsenceReportDto(
				a.Id, a.StudentId, a.Student.Name,
				a.Date, a.EndDate, a.Reason, a.Status, a.CreatedAt))
			.ToListAsync(cancellationToken);

		return Ok(reports);
	}

	[HttpGet]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<IReadOnlyList<AbsenceReportDto>>> GetAbsences(
		[FromQuery] Guid? classId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
		CancellationToken cancellationToken)
	{
		var query = db.AbsenceReports
			.AsNoTracking()
			.Include(a => a.Student)
			.AsQueryable();

		if (classId.HasValue)
		{
			query = query.Where(a => a.Student.ClassId == classId.Value);
		}

		if (from.HasValue)
		{
			query = query.Where(a => a.Date >= from.Value);
		}

		if (to.HasValue)
		{
			query = query.Where(a => a.Date <= to.Value);
		}

		var reports = await query
			.OrderByDescending(a => a.Date)
			.Select(a => new AbsenceReportDto(
				a.Id, a.StudentId, a.Student.Name,
				a.Date, a.EndDate, a.Reason, a.Status, a.CreatedAt))
			.ToListAsync(cancellationToken);

		return Ok(reports);
	}

	[HttpPost("{id:guid}/confirm")]
	public async Task<IActionResult> ConfirmAbsence(Guid id, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var staff = await db.Staff.AsNoTracking()
			.FirstOrDefaultAsync(s => s.KeycloakSubject == subject, cancellationToken);

		if (staff is null)
		{
			return Forbid();
		}

		var report = await db.AbsenceReports
			.Include(a => a.Student)
			.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

		if (report is null)
		{
			return NotFound();
		}

		var authResult = await authorization.AuthorizeAsync(User, report.Student.ClassId, new EditClassRequirement());
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		report.Status = AbsenceStatus.Confirmed;
		report.ConfirmedByStaffId = staff.Id;
		report.ConfirmedAt = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(cancellationToken);

		var staffName = staff.Name;
		var body = $"{staffName} har bekræftet {report.Student.Name}s fravær {report.Date:d. MMMM}";

		await notifications.CreateAsync(
			report.ReportedByParentId,
			RecipientType.Parent,
			NotificationType.AbsenceConfirmed,
			report.Id,
			body,
			cancellationToken);

		return NoContent();
	}

	[HttpPost("{id:guid}/dismiss")]
	public async Task<IActionResult> DismissAbsence(Guid id, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var staff = await db.Staff.AsNoTracking()
			.FirstOrDefaultAsync(s => s.KeycloakSubject == subject, cancellationToken);

		if (staff is null)
		{
			return Forbid();
		}

		var report = await db.AbsenceReports
			.Include(a => a.Student)
			.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

		if (report is null)
		{
			return NotFound();
		}

		var authResult = await authorization.AuthorizeAsync(User, report.Student.ClassId, new EditClassRequirement());
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		report.Status = AbsenceStatus.Dismissed;
		report.ConfirmedByStaffId = staff.Id;
		report.ConfirmedAt = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(cancellationToken);

		var staffName = staff.Name;
		var body = $"{staffName} har afvist {report.Student.Name}s fravær {report.Date:d. MMMM}";

		await notifications.CreateAsync(
			report.ReportedByParentId,
			RecipientType.Parent,
			NotificationType.AbsenceDismissed,
			report.Id,
			body,
			cancellationToken);

		return NoContent();
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = Roles.Parent)]
	public async Task<IActionResult> CancelAbsence(Guid id, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var parent = await db.Parents.AsNoTracking()
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, cancellationToken);

		if (parent is null)
		{
			return Forbid();
		}

		var report = await db.AbsenceReports
			.FirstOrDefaultAsync(a => a.Id == id && a.ReportedByParentId == parent.Id, cancellationToken);

		if (report is null)
		{
			return NotFound();
		}

		if (report.Status != AbsenceStatus.Reported)
		{
			return BadRequest(new { detail = "Kan ikke annullere et fravær der allerede er bekræftet eller afvist." });
		}

		db.AbsenceReports.Remove(report);
		await db.SaveChangesAsync(cancellationToken);

		return NoContent();
	}
}
