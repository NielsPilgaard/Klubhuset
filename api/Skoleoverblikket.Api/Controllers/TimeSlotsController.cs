using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Storage;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class TimeSlotsController(
	AppDbContext context,
	ITenantContext tenant,
	IObjectStorage storage,
	IAmazonS3 s3,
	IOptions<S3Options> s3Opts) : ControllerBase
{
	public record BreakDto(Guid Id, TimeOnly StartTime, int DurationMinutes);
	public record TemplateDto(Guid Id, int LessonDurationMinutes, TimeOnly DayStartTime, TimeOnly DayEndTime,
		string ActiveDays, IReadOnlyList<BreakDto> Breaks);

	private record BackupTimeSlotDto(Guid Id, Guid TenantId, Guid? ClassId, Guid? SchemaId, int SortOrder,
		TimeOnly StartTime, TimeOnly EndTime, string? Label, bool IsBreak);
	private record BackupSchemaSlotDto(Guid Id, Guid TenantId, Guid SchemaId, Guid TimeSlotId,
		DayOfWeek Weekday, Guid CourseId, Guid TeacherId, Guid? RoomId, Guid? AideId);
	private record BackupBreakDto(Guid Id, Guid TenantId, Guid TimeSlotTemplateId, TimeOnly StartTime, int DurationMinutes);
	private record BackupTemplateDto(Guid Id, Guid TenantId, int LessonDurationMinutes, TimeOnly DayStartTime,
		TimeOnly DayEndTime, string ActiveDays, IReadOnlyList<BackupBreakDto> Breaks);
	private record DefaultScheduleBackup(
		BackupTemplateDto Template,
		IReadOnlyList<BackupTimeSlotDto> SchoolLevelSlots,
		IReadOnlyList<BackupTimeSlotDto> SchemaLevelSlots,
		IReadOnlyList<BackupSchemaSlotDto> SchemaSlots);

	private static readonly JsonSerializerOptions BackupJsonOptions = new() { PropertyNameCaseInsensitive = true };

	public record UpsertBreakRequest(TimeOnly StartTime, int DurationMinutes);
	public record UpsertTemplateRequest(int LessonDurationMinutes, TimeOnly DayStartTime, TimeOnly DayEndTime,
		string ActiveDays, IReadOnlyList<UpsertBreakRequest> Breaks);

	[HttpGet("time-slot-template")]
	public async Task<ActionResult<TemplateDto>> GetTemplate(CancellationToken ct)
	{
		var timeSlotTemplate = await context.TimeSlotTemplates
						.AsNoTrackingWithIdentityResolution()
						.Include(t => t.Breaks)
						.FirstOrDefaultAsync(ct);

		return timeSlotTemplate is null
				   ? NotFound()
				   : Ok(ToTemplateDto(timeSlotTemplate));
	}

	[HttpPut("time-slot-template")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<TemplateDto>> UpsertTemplate([FromBody] UpsertTemplateRequest req, CancellationToken ct)
	{
		if (req.Breaks.Count > 0)
		{
			var breakValidationError = ValidateBreaksAgainstModules(req.DayStartTime, req.LessonDurationMinutes, req.Breaks);
			if (breakValidationError is not null)
			{
				return Problem(
					title: "Ugyldig pausekonfiguration",
					detail: breakValidationError,
					statusCode: 422);
			}
		}

		var timeSlotTemplate = await context.TimeSlotTemplates.Include(t => t.Breaks).FirstOrDefaultAsync(ct);
		if (timeSlotTemplate is null)
		{
			timeSlotTemplate = new TimeSlotTemplate { Id = Guid.NewGuid(), TenantId = tenant.TenantId };
			context.TimeSlotTemplates.Add(timeSlotTemplate);
		}

		// Back up current state to S3 before any destructive changes
		await CreateBackupAsync(timeSlotTemplate, ct);

		timeSlotTemplate.LessonDurationMinutes = req.LessonDurationMinutes;
		timeSlotTemplate.DayStartTime = req.DayStartTime;
		timeSlotTemplate.DayEndTime = req.DayEndTime;
		timeSlotTemplate.ActiveDays = req.ActiveDays;

		// Replace breaks — remove old ones cleanly then add new ones separately
		// to avoid EF Core double-tracking the deletes and raising a concurrency exception.
		var oldBreaks = timeSlotTemplate.Breaks.ToList();
		context.TimeSlotTemplateBreaks.RemoveRange(oldBreaks);
		timeSlotTemplate.Breaks.Clear();

		var newBreaks = req.Breaks.Select(b => new TimeSlotTemplateBreak
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			TimeSlotTemplateId = timeSlotTemplate.Id,
			StartTime = b.StartTime,
			DurationMinutes = b.DurationMinutes,
		}).ToList();

		context.TimeSlotTemplateBreaks.AddRange(newBreaks);

		// Regenerate school-level time slots (ClassId = null, SchemaId = null) from the template
		var existingSchoolSlots = await context.TimeSlots.Where(s => s.ClassId == null && s.SchemaId == null).ToListAsync(ct);
		context.TimeSlots.RemoveRange(existingSchoolSlots);

		var generatedSlots = GenerateSlotsFromTemplate(timeSlotTemplate, tenant.TenantId);
		context.TimeSlots.AddRange(generatedSlots);

		await context.SaveChangesAsync(ct);
		return Ok(ToTemplateDto(timeSlotTemplate));
	}

	[HttpPost("time-slot-template/restore")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<IActionResult> RestoreTemplate(CancellationToken ct)
	{
		var key = $"backups/{tenant.TenantId}/default-schedule-backup.json";

		GetObjectResponse s3Response;
		try
		{
			s3Response = await s3.GetObjectAsync(s3Opts.Value.DefaultBucketName, key, ct);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return Problem(
				title: "Ingen sikkerhedskopi fundet",
				detail: "Der findes ingen sikkerhedskopi at gendanne.",
				statusCode: 404);
		}

		DefaultScheduleBackup backup;
		try
		{
			using var stream = s3Response.ResponseStream;
			backup = await JsonSerializer.DeserializeAsync<DefaultScheduleBackup>(stream, BackupJsonOptions, ct)
				?? throw new InvalidOperationException("Backup JSON var null.");
		}
		catch (Exception ex)
		{
			return Problem(
				title: "Ugyldig sikkerhedskopi",
				detail: $"Sikkerhedskopien kunne ikke læses: {ex.Message}",
				statusCode: 422);
		}

		await using var transaction = await context.Database.BeginTransactionAsync(ct);
		try
		{
			var schoolSlots = await context.TimeSlots.Where(s => s.ClassId == null && s.SchemaId == null).ToListAsync(ct);
			context.TimeSlots.RemoveRange(schoolSlots);

			var schemaLevelSlots = await context.TimeSlots.Where(s => s.SchemaId != null).ToListAsync(ct);
			context.TimeSlots.RemoveRange(schemaLevelSlots);

			var schemaSlotRefs = await context.SchemaSlots.ToListAsync(ct);
			context.SchemaSlots.RemoveRange(schemaSlotRefs);

			await context.SaveChangesAsync(ct);

			var restoredSchoolSlots = backup.SchoolLevelSlots.Select(s => new TimeSlot
			{
				Id = s.Id,
				TenantId = s.TenantId,
				ClassId = s.ClassId,
				SchemaId = s.SchemaId,
				SortOrder = s.SortOrder,
				StartTime = s.StartTime,
				EndTime = s.EndTime,
				Label = s.Label,
				IsBreak = s.IsBreak,
			}).ToList();
			context.TimeSlots.AddRange(restoredSchoolSlots);

			var restoredSchemaSlots = backup.SchemaLevelSlots.Select(s => new TimeSlot
			{
				Id = s.Id,
				TenantId = s.TenantId,
				ClassId = s.ClassId,
				SchemaId = s.SchemaId,
				SortOrder = s.SortOrder,
				StartTime = s.StartTime,
				EndTime = s.EndTime,
				Label = s.Label,
				IsBreak = s.IsBreak,
			}).ToList();
			context.TimeSlots.AddRange(restoredSchemaSlots);

			var restoredSchemaSlotRefs = backup.SchemaSlots.Select(ss => new SchemaSlot
			{
				Id = ss.Id,
				TenantId = ss.TenantId,
				SchemaId = ss.SchemaId,
				TimeSlotId = ss.TimeSlotId,
				Weekday = ss.Weekday,
				CourseId = ss.CourseId,
				TeacherId = ss.TeacherId,
				RoomId = ss.RoomId,
				AideId = ss.AideId,
			}).ToList();
			context.SchemaSlots.AddRange(restoredSchemaSlotRefs);

			var template = await context.TimeSlotTemplates.Include(t => t.Breaks).FirstOrDefaultAsync(ct);
			if (template is not null)
			{
				template.LessonDurationMinutes = backup.Template.LessonDurationMinutes;
				template.DayStartTime = backup.Template.DayStartTime;
				template.DayEndTime = backup.Template.DayEndTime;
				template.ActiveDays = backup.Template.ActiveDays;

				var oldBreaks = template.Breaks.ToList();
				context.TimeSlotTemplateBreaks.RemoveRange(oldBreaks);
				template.Breaks.Clear();

				var restoredBreaks = backup.Template.Breaks.Select(b => new TimeSlotTemplateBreak
				{
					Id = b.Id,
					TenantId = b.TenantId,
					TimeSlotTemplateId = b.TimeSlotTemplateId,
					StartTime = b.StartTime,
					DurationMinutes = b.DurationMinutes,
				}).ToList();
				context.TimeSlotTemplateBreaks.AddRange(restoredBreaks);
			}

			await context.SaveChangesAsync(ct);
			await transaction.CommitAsync(ct);
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync(ct);
			return Problem(
				title: "Gendannelse mislykkedes",
				detail: $"Kunne ikke gendanne sikkerhedskopien: {ex.Message}",
				statusCode: 500);
		}

		return NoContent();
	}

	private async Task CreateBackupAsync(TimeSlotTemplate currentTemplate, CancellationToken ct)
	{
		var schoolLevelSlots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == null && s.SchemaId == null)
			.ToListAsync(ct);

		var schemaLevelSlots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.SchemaId != null)
			.ToListAsync(ct);

		var schemaSlots = await context.SchemaSlots
			.AsNoTracking()
			.ToListAsync(ct);

		var backup = new DefaultScheduleBackup(
			Template: new BackupTemplateDto(
				Id: currentTemplate.Id,
				TenantId: currentTemplate.TenantId,
				LessonDurationMinutes: currentTemplate.LessonDurationMinutes,
				DayStartTime: currentTemplate.DayStartTime,
				DayEndTime: currentTemplate.DayEndTime,
				ActiveDays: currentTemplate.ActiveDays,
				Breaks: currentTemplate.Breaks.Select(b => new BackupBreakDto(
					Id: b.Id,
					TenantId: b.TenantId,
					TimeSlotTemplateId: b.TimeSlotTemplateId,
					StartTime: b.StartTime,
					DurationMinutes: b.DurationMinutes)).ToList()),
			SchoolLevelSlots: schoolLevelSlots.Select(s => new BackupTimeSlotDto(
				Id: s.Id, TenantId: s.TenantId, ClassId: s.ClassId, SchemaId: s.SchemaId,
				SortOrder: s.SortOrder, StartTime: s.StartTime, EndTime: s.EndTime,
				Label: s.Label, IsBreak: s.IsBreak)).ToList(),
			SchemaLevelSlots: schemaLevelSlots.Select(s => new BackupTimeSlotDto(
				Id: s.Id, TenantId: s.TenantId, ClassId: s.ClassId, SchemaId: s.SchemaId,
				SortOrder: s.SortOrder, StartTime: s.StartTime, EndTime: s.EndTime,
				Label: s.Label, IsBreak: s.IsBreak)).ToList(),
			SchemaSlots: schemaSlots.Select(ss => new BackupSchemaSlotDto(
				Id: ss.Id, TenantId: ss.TenantId, SchemaId: ss.SchemaId,
				TimeSlotId: ss.TimeSlotId, Weekday: ss.Weekday,
				CourseId: ss.CourseId, TeacherId: ss.TeacherId,
				RoomId: ss.RoomId, AideId: ss.AideId)).ToList());

		var json = JsonSerializer.Serialize(backup);
		var key = $"backups/{tenant.TenantId}/default-schedule-backup.json";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		await storage.UploadAsync(key, "application/json", stream, ct);
	}

	/// <summary>
	/// Returns an error message if any break start time falls in the middle of a lesson module.
	/// Walks the timeline accounting for earlier breaks shifting subsequent module boundaries.
	/// </summary>
	private static string? ValidateBreaksAgainstModules(TimeOnly dayStart, int lessonDuration, IReadOnlyList<UpsertBreakRequest> breaks)
	{
		var sortedBreaks = breaks.OrderBy(b => b.StartTime).ToList();

		foreach (var @break in sortedBreaks)
		{
			if (@break.StartTime < dayStart)
			{
				return $"Pausen kl. {@break.StartTime:HH\\:mm} starter før skoledagen.";
			}

			// Walk the timeline from dayStart, honouring earlier break durations,
			// to determine whether this break lands on a module boundary.
			var cursor = dayStart;
			var moduleNumber = 1;
			while (cursor < @break.StartTime)
			{
				var prevBreak = sortedBreaks.FirstOrDefault(pb => pb.StartTime == cursor && pb != @break);
				if (prevBreak is not null)
				{
					cursor = cursor.AddMinutes(prevBreak.DurationMinutes);
					continue;
				}

				var nextBoundary = cursor.AddMinutes(lessonDuration);
				if (nextBoundary > @break.StartTime)
				{
					return $"Pausen kl. {@break.StartTime:HH\\:mm} falder midt i modul {moduleNumber} ({cursor:HH\\:mm}–{nextBoundary:HH\\:mm}). Pauser skal starte præcis ved en lektionsovergang.";
				}

				cursor = nextBoundary;
				if (cursor < @break.StartTime)
				{
					moduleNumber++;
				}
			}
		}

		return null;
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
	public record UpsertTimeSlotRequest(int SortOrder, TimeOnly StartTime, TimeOnly EndTime, string? Label, bool IsBreak = false);

	[HttpGet("classes/{classId:guid}/time-slots")]
	public async Task<ActionResult<List<TimeSlotDto>>> GetForClass(Guid classId, CancellationToken ct)
	{
		var slots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == classId && s.SchemaId == null)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		if (slots.Count > 0)
		{
			return Ok(slots);
		}

		// Fall back to school-level time slots when the class has no overrides
		var schoolSlots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == null && s.SchemaId == null)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		return Ok(schoolSlots);
	}

	[HttpGet("classes/{classId:guid}/schemas/{schemaId:guid}/time-slots")]
	public async Task<ActionResult<List<TimeSlotDto>>> GetForSchema(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var schemaExists = await context.Schemas.AnyAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (!schemaExists)
		{
			return NotFound();
		}

		// Schema-level slots take precedence
		var schemaSlots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.SchemaId == schemaId)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		if (schemaSlots.Count > 0)
		{
			return Ok(schemaSlots);
		}

		// Fall back to class-level, then school-level
		var classSlots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == classId && s.SchemaId == null)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		if (classSlots.Count > 0)
		{
			return Ok(classSlots);
		}

		var schoolSlots = await context.TimeSlots
			.AsNoTracking()
			.Where(s => s.ClassId == null && s.SchemaId == null)
			.OrderBy(s => s.SortOrder)
			.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak))
			.ToListAsync(ct);

		return Ok(schoolSlots);
	}

	[HttpPut("classes/{classId:guid}/schemas/{schemaId:guid}/time-slots")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<List<TimeSlotDto>>> ReplaceForSchema(
		Guid classId, Guid schemaId,
		[FromBody] IReadOnlyList<UpsertTimeSlotRequest> req,
		CancellationToken ct)
	{
		var schema = await context.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		var existing = await context.TimeSlots.Where(s => s.SchemaId == schemaId).ToListAsync(ct);
		context.TimeSlots.RemoveRange(existing);

		var newSlots = req.Select(r => new TimeSlot
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = classId,
			SchemaId = schemaId,
			SortOrder = r.SortOrder,
			StartTime = r.StartTime,
			EndTime = r.EndTime,
			Label = r.Label,
			IsBreak = r.IsBreak,
		}).ToList();

		context.TimeSlots.AddRange(newSlots);
		await context.SaveChangesAsync(ct);

		var result = newSlots.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak));
		return Ok(result);
	}

	[HttpPut("classes/{classId:guid}/time-slots")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<List<TimeSlotDto>>> ReplaceForClass(Guid classId, [FromBody] IReadOnlyList<UpsertTimeSlotRequest> req, CancellationToken ct)
	{
		var exists = await context.Classes.AnyAsync(c => c.Id == classId, ct);
		if (!exists)
		{
			return NotFound();
		}

		var existing = await context.TimeSlots.Where(s => s.ClassId == classId && s.SchemaId == null).ToListAsync(ct);
		context.TimeSlots.RemoveRange(existing);

		var newSlots = req.Select(upsertTimeSlotRequest => new TimeSlot
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = classId,
			SortOrder = upsertTimeSlotRequest.SortOrder,
			StartTime = upsertTimeSlotRequest.StartTime,
			EndTime = upsertTimeSlotRequest.EndTime,
			Label = upsertTimeSlotRequest.Label,
			IsBreak = upsertTimeSlotRequest.IsBreak,
		}).ToList();

		context.TimeSlots.AddRange(newSlots);
		await context.SaveChangesAsync(ct);

		var result = newSlots.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label, s.IsBreak));
		return Ok(result);
	}

	[HttpGet("time-slots")]
	public async Task<ActionResult<List<TimeSlotDto>>> GetSchoolLevelSlots(CancellationToken ct)
	{
		var slots = await context.TimeSlots
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
