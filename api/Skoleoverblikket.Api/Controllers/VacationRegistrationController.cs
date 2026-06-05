using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/vacation-registration")]
[Authorize]
public sealed class VacationRegistrationController(AppDbContext db, ITenantContext tenant, INotificationService notifications) : ControllerBase
{
	// ── DTOs ────────────────────────────────────────────────────────────────

	public record WindowDto(
		Guid Id,
		string Title,
		DateOnly RegistrationDeadline,
		DateOnly CareStartDate,
		DateOnly CareEndDate,
		VacationRegistrationGranularity Granularity,
		bool IsOpen,
		int EntryCount,
		DateTimeOffset CreatedAt);

	public record CreateWindowRequest(
		string Title,
		DateOnly RegistrationDeadline,
		DateOnly CareStartDate,
		DateOnly CareEndDate,
		VacationRegistrationGranularity Granularity,
		bool IsOpen);

	public record UpdateWindowRequest(
		string Title,
		DateOnly RegistrationDeadline,
		DateOnly CareStartDate,
		DateOnly CareEndDate,
		VacationRegistrationGranularity Granularity,
		bool IsOpen);

	public record EntryDto(
		Guid Id,
		Guid StudentId,
		string StudentName,
		string ClassName,
		Guid SubmittedByParentId,
		string SubmittedByParentName,
		string[] SelectedDates,
		string? Note,
		DateTimeOffset SubmittedAt,
		DateTimeOffset UpdatedAt);

	public record UpsertEntryRequest(string[] SelectedDates, string? Note);

	public record MyEntryDto(
		Guid StudentId,
		string StudentName,
		string[] SelectedDates,
		string? Note,
		DateTimeOffset? SubmittedAt);

	// ── Admin: windows ───────────────────────────────────────────────────────

	[HttpGet]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<IReadOnlyList<WindowDto>>> GetWindows(CancellationToken cancellationToken)
	{
		var windows = await db.VacationRegistrationWindows
			.AsNoTracking()
			.OrderByDescending(w => w.CreatedAt)
			.Select(w => new WindowDto(
				w.Id, w.Title, w.RegistrationDeadline, w.CareStartDate, w.CareEndDate,
				w.Granularity, w.IsOpen, w.Entries.Count, w.CreatedAt))
			.ToListAsync(cancellationToken);

		return Ok(windows);
	}

	[HttpPost]
	[Authorize(Roles = Roles.Admin)]
	public async Task<IActionResult> CreateWindow([FromBody] CreateWindowRequest req, CancellationToken cancellationToken)
	{
		var window = new VacationRegistrationWindow
		{
			TenantId = tenant.TenantId,
			Title = req.Title,
			RegistrationDeadline = req.RegistrationDeadline,
			CareStartDate = req.CareStartDate,
			CareEndDate = req.CareEndDate,
			Granularity = req.Granularity,
			IsOpen = req.IsOpen,
		};

		db.VacationRegistrationWindows.Add(window);
		await db.SaveChangesAsync(cancellationToken);

		if (req.IsOpen)
		{
			await NotifyAllParentsAsync(window, cancellationToken);
		}

		return CreatedAtAction(nameof(GetWindows), new { }, null);
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<IActionResult> UpdateWindow(Guid id, [FromBody] UpdateWindowRequest req, CancellationToken cancellationToken)
	{
		var window = await db.VacationRegistrationWindows
			.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

		if (window is null)
		{
			return NotFound();
		}

		var wasOpen = window.IsOpen;

		window.Title = req.Title;
		window.RegistrationDeadline = req.RegistrationDeadline;
		window.CareStartDate = req.CareStartDate;
		window.CareEndDate = req.CareEndDate;
		window.Granularity = req.Granularity;
		window.IsOpen = req.IsOpen;

		await db.SaveChangesAsync(cancellationToken);

		if (!wasOpen && req.IsOpen)
		{
			await NotifyAllParentsAsync(window, cancellationToken);
		}

		return NoContent();
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<IActionResult> DeleteWindow(Guid id, CancellationToken cancellationToken)
	{
		var window = await db.VacationRegistrationWindows
			.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

		if (window is null)
		{
			return NotFound();
		}

		db.VacationRegistrationWindows.Remove(window);
		await db.SaveChangesAsync(cancellationToken);

		return NoContent();
	}

	[HttpGet("{id:guid}/entries")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<IReadOnlyList<EntryDto>>> GetEntries(Guid id, CancellationToken cancellationToken)
	{
		var windowExists = await db.VacationRegistrationWindows
			.AnyAsync(w => w.Id == id, cancellationToken);

		if (!windowExists)
		{
			return NotFound();
		}

		var entries = await db.VacationRegistrationEntries
			.AsNoTracking()
			.Include(e => e.Student).ThenInclude(s => s.Class)
			.Include(e => e.SubmittedByParent)
			.Where(e => e.WindowId == id)
			.OrderBy(e => e.Student.Name)
			.Select(e => new EntryDto(
				e.Id,
				e.StudentId,
				e.Student.Name,
				e.Student.Class.Name,
				e.SubmittedByParentId,
				e.SubmittedByParent.Name,
				e.SelectedDates == string.Empty ? Array.Empty<string>() : e.SelectedDates.Split(',', StringSplitOptions.RemoveEmptyEntries),
				e.Note,
				e.SubmittedAt,
				e.UpdatedAt))
			.ToListAsync(cancellationToken);

		return Ok(entries);
	}

	// ── Parent: open windows ─────────────────────────────────────────────────

	[HttpGet("open")]
	[Authorize(Roles = Roles.Parent)]
	public async Task<ActionResult<IReadOnlyList<WindowDto>>> GetOpenWindows(CancellationToken cancellationToken)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);

		var windows = await db.VacationRegistrationWindows
			.AsNoTracking()
			.Where(w => w.IsOpen && w.RegistrationDeadline >= today)
			.OrderBy(w => w.RegistrationDeadline)
			.Select(w => new WindowDto(
				w.Id, w.Title, w.RegistrationDeadline, w.CareStartDate, w.CareEndDate,
				w.Granularity, w.IsOpen, w.Entries.Count, w.CreatedAt))
			.ToListAsync(cancellationToken);

		return Ok(windows);
	}

	[HttpGet("{id:guid}/my-entries")]
	[Authorize(Roles = Roles.Parent)]
	public async Task<ActionResult<IReadOnlyList<MyEntryDto>>> GetMyEntries(Guid id, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var parent = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students)
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, cancellationToken);

		if (parent is null)
		{
			return Forbid();
		}

		var windowExists = await db.VacationRegistrationWindows
			.AnyAsync(w => w.Id == id, cancellationToken);

		if (!windowExists)
		{
			return NotFound();
		}

		var studentIds = parent.Students.Select(s => s.Id).ToHashSet();

		var existingEntries = await db.VacationRegistrationEntries
			.AsNoTracking()
			.Where(e => e.WindowId == id && studentIds.Contains(e.StudentId))
			.ToDictionaryAsync(e => e.StudentId, cancellationToken);

		var result = parent.Students
			.OrderBy(s => s.Name)
			.Select(s =>
			{
				existingEntries.TryGetValue(s.Id, out var entry);
				return new MyEntryDto(
					s.Id,
					s.Name,
					entry is null || entry.SelectedDates == string.Empty
						? []
						: entry.SelectedDates.Split(',', StringSplitOptions.RemoveEmptyEntries),
					entry?.Note,
					entry?.SubmittedAt);
			})
			.ToList();

		return Ok(result);
	}

	[HttpPut("{id:guid}/entries/{studentId:guid}")]
	[Authorize(Roles = Roles.Parent)]
	public async Task<IActionResult> UpsertEntry(Guid id, Guid studentId, [FromBody] UpsertEntryRequest req, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var parent = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students)
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, cancellationToken);

		if (parent is null)
		{
			return Forbid();
		}

		if (!parent.Students.Any(s => s.Id == studentId))
		{
			return Forbid();
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);

		var window = await db.VacationRegistrationWindows
			.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

		if (window is null)
		{
			return NotFound();
		}

		if (!window.IsOpen || window.RegistrationDeadline < today)
		{
			return Problem(
				title: "Vinduet er lukket",
				detail: "Ferietilmeldingen er ikke åben for registreringer.",
				statusCode: StatusCodes.Status409Conflict);
		}

		var entry = await db.VacationRegistrationEntries
			.FirstOrDefaultAsync(e => e.WindowId == id && e.StudentId == studentId, cancellationToken);

		var selectedDates = string.Join(',', req.SelectedDates);

		if (entry is null)
		{
			entry = new VacationRegistrationEntry
			{
				TenantId = parent.TenantId,
				WindowId = id,
				StudentId = studentId,
				SubmittedByParentId = parent.Id,
				SelectedDates = selectedDates,
				Note = req.Note,
			};
			db.VacationRegistrationEntries.Add(entry);
		}
		else
		{
			entry.SelectedDates = selectedDates;
			entry.Note = req.Note;
			entry.UpdatedAt = DateTimeOffset.UtcNow;
		}

		await db.SaveChangesAsync(cancellationToken);

		return NoContent();
	}

	[HttpDelete("{id:guid}/entries/{studentId:guid}")]
	[Authorize(Roles = Roles.Parent)]
	public async Task<IActionResult> DeleteEntry(Guid id, Guid studentId, CancellationToken cancellationToken)
	{
		var subject = User.GetKeycloakSubject();

		var parent = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students)
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, cancellationToken);

		if (parent is null)
		{
			return Forbid();
		}

		if (!parent.Students.Any(s => s.Id == studentId))
		{
			return Forbid();
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);

		var window = await db.VacationRegistrationWindows
			.AsNoTracking()
			.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

		if (window is null)
		{
			return NotFound();
		}

		if (!window.IsOpen || window.RegistrationDeadline < today)
		{
			return Problem(
				title: "Vinduet er lukket",
				detail: "Ferietilmeldingen er ikke åben for ændringer.",
				statusCode: StatusCodes.Status409Conflict);
		}

		var entry = await db.VacationRegistrationEntries
			.FirstOrDefaultAsync(e => e.WindowId == id && e.StudentId == studentId, cancellationToken);

		if (entry is null)
		{
			return NotFound();
		}

		db.VacationRegistrationEntries.Remove(entry);
		await db.SaveChangesAsync(cancellationToken);

		return NoContent();
	}

	// ── Helpers ──────────────────────────────────────────────────────────────

	private async Task NotifyAllParentsAsync(VacationRegistrationWindow window, CancellationToken cancellationToken)
	{
		var allParents = await db.Parents.AsNoTracking().ToListAsync(cancellationToken);

		foreach (var parent in allParents)
		{
			await notifications.CreateAsync(
				parent.Id,
				RecipientType.Parent,
				NotificationType.VacationRegistrationOpened,
				window.Id,
				$"Ferietilmelding åben: {window.Title}. Frist: {window.RegistrationDeadline:d. MMMM}.",
				cancellationToken);
		}
	}
}
