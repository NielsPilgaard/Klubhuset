using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class TimeSlotsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
	public record BreakDto(Guid Id, TimeOnly StartTime, int DurationMinutes);
	public record TemplateDto(Guid Id, int LessonDurationMinutes, TimeOnly DayStartTime, TimeOnly DayEndTime,
		string ActiveDays, IReadOnlyList<BreakDto> Breaks);

	public record UpsertBreakRequest(TimeOnly StartTime, int DurationMinutes);
	public record UpsertTemplateRequest(int LessonDurationMinutes, TimeOnly DayStartTime, TimeOnly DayEndTime,
		string ActiveDays, IReadOnlyList<UpsertBreakRequest> Breaks);

	[HttpGet("time-slot-template")]
	public async Task<ActionResult<TemplateDto>> GetTemplate(CancellationToken ct)
	{
		var t = await db.TimeSlotTemplates
			.AsNoTrackingWithIdentityResolution()
			.Include(t => t.Breaks)
			.FirstOrDefaultAsync(ct);
		if (t is null)
		{
			return NotFound();
		}

		return Ok(ToTemplateDto(t));
	}

	[HttpPut("time-slot-template")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<TemplateDto>> UpsertTemplate([FromBody] UpsertTemplateRequest req, CancellationToken ct)
	{
		// Validate that every break starts exactly on a module boundary
		// TODO: Uncomment
		// if (req.Breaks.Count > 0)
		// {
		// 	var breakValidationError = ValidateBreaksAgainstModules(req.DayStartTime, req.LessonDurationMinutes, req.Breaks);
		// 	if (breakValidationError is not null)
		// 	{
		// 		return Problem(
		// 			title: "Ugyldig pausekonfiguration",
		// 			detail: breakValidationError,
		// 			statusCode: 422);
		// 	}
		// }

		var timeSlotTemplate = await db.TimeSlotTemplates.Include(t => t.Breaks).FirstOrDefaultAsync(ct);
		if (timeSlotTemplate is null)
		{
			timeSlotTemplate = new TimeSlotTemplate { Id = Guid.NewGuid(), TenantId = tenant.TenantId };
			db.TimeSlotTemplates.Add(timeSlotTemplate);
		}

		timeSlotTemplate.LessonDurationMinutes = req.LessonDurationMinutes;
		timeSlotTemplate.DayStartTime = req.DayStartTime;
		timeSlotTemplate.DayEndTime = req.DayEndTime;
		timeSlotTemplate.ActiveDays = req.ActiveDays;

		// Replace breaks — remove old ones cleanly then add new ones separately
		// to avoid EF Core double-tracking the deletes and raising a concurrency exception.
		var oldBreaks = timeSlotTemplate.Breaks.ToList();
		db.TimeSlotTemplateBreaks.RemoveRange(oldBreaks);
		timeSlotTemplate.Breaks.Clear();

		var newBreaks = req.Breaks.Select(b => new TimeSlotTemplateBreak
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			TimeSlotTemplateId = timeSlotTemplate.Id,
			StartTime = b.StartTime,
			DurationMinutes = b.DurationMinutes,
		}).ToList();
		db.TimeSlotTemplateBreaks.AddRange(newBreaks);

		// Regenerate school-level time slots (ClassId = null) from the template
		var existingSchoolSlots = await db.TimeSlots.Where(s => s.ClassId == null).ToListAsync(ct);
		db.TimeSlots.RemoveRange(existingSchoolSlots);

		var generatedSlots = GenerateSlotsFromTemplate(timeSlotTemplate, tenant.TenantId);
		db.TimeSlots.AddRange(generatedSlots);

		await db.SaveChangesAsync(ct);
		return Ok(ToTemplateDto(timeSlotTemplate));
	}

	private static List<TimeSlot> GenerateSlotsFromTemplate(TimeSlotTemplate t, Guid tenantId)
	{
		var slots = new List<TimeSlot>();
		var breaks = t.Breaks.OrderBy(b => b.StartTime).ToList();
		var current = t.DayStartTime;
		var sortOrder = 1;

		while (current < t.DayEndTime)
		{
			// Emit a break row if one starts exactly here
			var breakHere = breaks.FirstOrDefault(b => b.StartTime == current);
			if (breakHere is not null)
			{
				slots.Add(new TimeSlot
				{
					Id = Guid.NewGuid(),
					TenantId = tenantId,
					ClassId = null,
					SortOrder = sortOrder++,
					StartTime = breakHere.StartTime,
					EndTime = breakHere.StartTime.AddMinutes(breakHere.DurationMinutes),
					Label = "Pause",
					IsBreak = true,
				});
				current = breakHere.StartTime.AddMinutes(breakHere.DurationMinutes);
				continue;
			}

			var lessonEnd = current.AddMinutes(t.LessonDurationMinutes);

			if (lessonEnd > t.DayEndTime)
			{
				break;
			}

			slots.Add(new TimeSlot
			{
				Id = Guid.NewGuid(),
				TenantId = tenantId,
				ClassId = null,
				SortOrder = sortOrder++,
				StartTime = current,
				EndTime = lessonEnd,
				IsBreak = false,
			});

			current = lessonEnd;
		}

		return slots;
	}

	public record TimeSlotDto(Guid Id, Guid? ClassId, int SortOrder, TimeOnly StartTime, TimeOnly EndTime, string? Label, bool IsBreak);
	public record UpsertTimeSlotRequest(int SortOrder, TimeOnly StartTime, TimeOnly EndTime, string? Label);

	[HttpGet("classes/{classId:guid}/time-slots")]
	public async Task<ActionResult<List<TimeSlotDto>>> GetForClass(Guid classId, CancellationToken ct)
	{
		var slots = await db.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == classId)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		if (slots.Count > 0)
		{
			return Ok(slots);
		}

		// Fall back to school-level time slots when the class has no overrides
		var schoolSlots = await db.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == null)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		return Ok(schoolSlots);
	}

	[HttpPut("classes/{classId:guid}/time-slots")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<List<TimeSlotDto>>> ReplaceForClass(Guid classId, [FromBody] IReadOnlyList<UpsertTimeSlotRequest> req, CancellationToken ct)
	{
		// Verify class belongs to tenant
		var exists = await db.Classes.AnyAsync(c => c.Id == classId, ct);
		if (!exists)
		{
			return NotFound();
		}

		var existing = await db.TimeSlots.Where(s => s.ClassId == classId).ToListAsync(ct);
		db.TimeSlots.RemoveRange(existing);

		var newSlots = req.Select(upsertTimeSlotRequest => new TimeSlot
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = classId,
			SortOrder = upsertTimeSlotRequest.SortOrder,
			StartTime = upsertTimeSlotRequest.StartTime,
			EndTime = upsertTimeSlotRequest.EndTime,
			Label = upsertTimeSlotRequest.Label,
		}).ToList();

		db.TimeSlots.AddRange(newSlots);
		await db.SaveChangesAsync(ct);

		var result = newSlots.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak));
		return Ok(result);
	}

	[HttpGet("time-slots")]
	public async Task<ActionResult<List<TimeSlotDto>>> GetSchoolLevelSlots(CancellationToken ct)
	{
		var slots = await db.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == null)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		return Ok(slots);
	}

	private static TemplateDto ToTemplateDto(TimeSlotTemplate t) => new(
		t.Id, t.LessonDurationMinutes, t.DayStartTime, t.DayEndTime, t.ActiveDays,
		t.Breaks.OrderBy(b => b.StartTime).Select(b => new BreakDto(b.Id, b.StartTime, b.DurationMinutes)).ToList());
}
