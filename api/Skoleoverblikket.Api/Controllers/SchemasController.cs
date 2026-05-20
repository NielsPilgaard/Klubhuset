using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/classes/{classId:guid}/schemas")]
[Authorize]
public sealed class SchemasController(AppDbContext db, ITenantContext tenant, ConflictDetectionService conflicts, IAuthorizationService authz)
	: ControllerBase
{
	public record SchemaDto(Guid Id, Guid ClassId, string Name, DateOnly? StartDate, DateOnly? EndDate);

	public record SetDateRangeRequest(DateOnly? StartDate, DateOnly? EndDate);

	public record SlotDto(
		Guid Id,
		Guid TimeSlotId,
		DayOfWeek Weekday,
		Guid CourseId,
		string CourseName,
		Guid TeacherId,
		string TeacherName,
		Guid? RoomId,
		string? RoomName,
		Guid? AideId,
		string? AideName);

	public record SchemaDetailDto(
		SchemaDto Schema,
		IReadOnlyList<SlotDto> Slots,
		IReadOnlyList<ConflictInfo> Conflicts);

	public record SlotsAndConflictsDto(IReadOnlyList<SlotDto> Slots, IReadOnlyList<ConflictInfo> Conflicts);

	public record CreateSchemaRequest([Required][MinLength(1)] string Name, Guid? CopyTimeSlotsFromSchemaId = null);

	public record CopySchemaRequest([Required][MinLength(1)] string Name);

	public record RenameSchemaRequest([Required][MinLength(1)] string Name);

	public record UpsertSlotRequest(
		[Required] Guid TimeSlotId,
		DayOfWeek Weekday,
		[Required] Guid CourseId,
		[Required] Guid TeacherId,
		Guid? RoomId,
		Guid? AideId);

	[HttpGet]
	[Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
	public async Task<ActionResult<List<SchemaDto>>> GetAll(Guid classId, CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.ParentClassAccess);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var schemas = await db.Schemas
							  .AsNoTracking()
							  .Where(s => s.ClassId == classId)
							  .OrderByDescending(s => s.CreatedAt)
							  .Select(s => new SchemaDto(s.Id, s.ClassId, s.Name, s.StartDate, s.EndDate))
							  .ToListAsync(ct);

		return Ok(schemas);
	}

	[HttpGet("{schemaId:guid}")]
	public async Task<ActionResult<SchemaDetailDto>> GetById(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		var slots = await GetSlotDtos(schemaId, ct);
		var conflictList = await conflicts.DetectAsync(schemaId, ct);

		return Ok(new SchemaDetailDto(
					  new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.StartDate, schema.EndDate),
					  slots,
					  conflictList));
	}

	[HttpPost]
	public async Task<ActionResult<SchemaDto>> Create(Guid classId, [FromBody] CreateSchemaRequest req,
		CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var classExists = await db.Classes.AnyAsync(c => c.Id == classId, ct);
		if (!classExists)
		{
			return NotFound();
		}

		var schema = new Schema
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = classId,
			Name = req.Name,
		};

		db.Schemas.Add(schema);

		// Copy time slots from source schema if requested
		if (req.CopyTimeSlotsFromSchemaId.HasValue)
		{
			var sourceSlots = await db.TimeSlots
				.AsNoTracking()
				.Where(s => s.SchemaId == req.CopyTimeSlotsFromSchemaId.Value)
				.ToListAsync(ct);

			foreach (var slot in sourceSlots)
			{
				db.TimeSlots.Add(new TimeSlot
				{
					Id = Guid.NewGuid(),
					TenantId = tenant.TenantId,
					ClassId = classId,
					SchemaId = schema.Id,
					SortOrder = slot.SortOrder,
					StartTime = slot.StartTime,
					EndTime = slot.EndTime,
					Label = slot.Label,
					IsBreak = slot.IsBreak,
				});
			}
		}

		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById),
							   new { classId, schemaId = schema.Id },
							   new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.StartDate, schema.EndDate));
	}

	[HttpPut("{schemaId:guid}/daterange")]
	public async Task<ActionResult<SchemaDto>> SetDateRange(Guid classId, Guid schemaId,
		[FromBody] SetDateRangeRequest req, CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		if (req.StartDate.HasValue && req.EndDate.HasValue && req.StartDate.Value > req.EndDate.Value)
		{
			return Problem(
				title: "Ugyldig datoperiode",
				detail: "Startdato skal være før eller lig med slutdato.",
				statusCode: 422);
		}

		schema.StartDate = req.StartDate;
		schema.EndDate = req.EndDate;

		await db.SaveChangesAsync(ct);
		return Ok(new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.StartDate, schema.EndDate));
	}

	[HttpPost("{schemaId:guid}/copy")]
	public async Task<ActionResult<SchemaDto>> Copy(Guid classId, Guid schemaId,
		[FromBody] CopySchemaRequest req, CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var source = await db.Schemas
							 .Include(s => s.Slots)
							 .FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);

		if (source is null)
		{
			return NotFound();
		}

		var copy = new Schema
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = classId,
			Name = req.Name,
		};

		db.Schemas.Add(copy);

		foreach (var slot in source.Slots)
		{
			db.SchemaSlots.Add(new SchemaSlot
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				SchemaId = copy.Id,
				TimeSlotId = slot.TimeSlotId,
				Weekday = slot.Weekday,
				CourseId = slot.CourseId,
				TeacherId = slot.TeacherId,
				RoomId = slot.RoomId,
				AideId = slot.AideId,
			});
		}

		// Copy schema-level time slots
		var sourceTimeSlots = await db.TimeSlots.AsNoTracking()
			.Where(s => s.SchemaId == schemaId).ToListAsync(ct);
		foreach (var ts in sourceTimeSlots)
		{
			db.TimeSlots.Add(new TimeSlot
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				ClassId = classId,
				SchemaId = copy.Id,
				SortOrder = ts.SortOrder,
				StartTime = ts.StartTime,
				EndTime = ts.EndTime,
				Label = ts.Label,
				IsBreak = ts.IsBreak,
			});
		}

		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById),
							   new { classId, schemaId = copy.Id },
							   new SchemaDto(copy.Id, copy.ClassId, copy.Name, copy.StartDate, copy.EndDate));
	}

	[HttpPost("{schemaId:guid}/copy-to/{targetClassId:guid}")]
	public async Task<ActionResult<SchemaDto>> CopyToClass(Guid classId, Guid schemaId, Guid targetClassId,
		[FromBody] CopySchemaRequest req, CancellationToken ct)
	{
		var sourceAuthResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!sourceAuthResult.Succeeded)
		{
			return Forbid();
		}

		var authResult = await authz.AuthorizeAsync(User, targetClassId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var source = await db.Schemas
							 .Include(s => s.Slots)
							 .FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (source is null)
		{
			return NotFound();
		}

		var targetExists = await db.Classes.AnyAsync(c => c.Id == targetClassId, ct);
		if (!targetExists)
		{
			return NotFound();
		}

		var copy = new Schema
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ClassId = targetClassId,
			Name = req.Name,
		};
		db.Schemas.Add(copy);

		foreach (var slot in source.Slots)
		{
			db.SchemaSlots.Add(new SchemaSlot
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				SchemaId = copy.Id,
				TimeSlotId = slot.TimeSlotId,
				Weekday = slot.Weekday,
				CourseId = slot.CourseId,
				TeacherId = slot.TeacherId,
				RoomId = slot.RoomId,
				AideId = slot.AideId,
			});
		}

		// Copy schema-level time slots
		var sourceTimeSlotsForClass = await db.TimeSlots.AsNoTracking()
			.Where(s => s.SchemaId == schemaId).ToListAsync(ct);
		foreach (var ts in sourceTimeSlotsForClass)
		{
			db.TimeSlots.Add(new TimeSlot
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				ClassId = targetClassId,
				SchemaId = copy.Id,
				SortOrder = ts.SortOrder,
				StartTime = ts.StartTime,
				EndTime = ts.EndTime,
				Label = ts.Label,
				IsBreak = ts.IsBreak,
			});
		}

		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById),
							   new { classId = targetClassId, schemaId = copy.Id },
							   new SchemaDto(copy.Id, copy.ClassId, copy.Name, copy.StartDate, copy.EndDate));
	}

	[HttpPut("{schemaId:guid}/rename")]
	public async Task<ActionResult<SchemaDto>> Rename(Guid classId, Guid schemaId,
		[FromBody] RenameSchemaRequest req, CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		schema.Name = req.Name;
		await db.SaveChangesAsync(ct);
		return Ok(new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.StartDate, schema.EndDate));
	}

	[HttpDelete("{schemaId:guid}")]
	public async Task<ActionResult> Delete(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		db.Schemas.Remove(schema);
		await db.SaveChangesAsync(ct);
		return NoContent();
	}

	[HttpGet("{schemaId:guid}/slots")]
	public async Task<ActionResult<List<SlotDto>>> GetSlots(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var schemaExists = await db.Schemas.AnyAsync(s => s.Id == schemaId && s.ClassId == classId, ct);

		return schemaExists
				   ? Ok(await GetSlotDtos(schemaId, ct))
				   : NotFound();
	}

	[HttpPut("{schemaId:guid}/slots")]
	public async Task<ActionResult<SlotsAndConflictsDto>> UpsertSlot(Guid classId, Guid schemaId,
		[FromBody] UpsertSlotRequest req, CancellationToken ct)
	{
		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		var slot = await db.SchemaSlots.FirstOrDefaultAsync(
					   s => s.SchemaId == schemaId && s.TimeSlotId == req.TimeSlotId && s.Weekday == req.Weekday,
					   ct);

		if (slot is null)
		{
			slot = new SchemaSlot
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				SchemaId = schemaId,
				TimeSlotId = req.TimeSlotId,
				Weekday = req.Weekday,
				CourseId = req.CourseId,
				TeacherId = req.TeacherId,
				RoomId = req.RoomId,
				AideId = req.AideId,
			};

			db.SchemaSlots.Add(slot);
		}
		else
		{
			slot.CourseId = req.CourseId;
			slot.TeacherId = req.TeacherId;
			slot.RoomId = req.RoomId;
			slot.AideId = req.AideId;
		}

		await db.SaveChangesAsync(ct);

		// Return updated slot list + conflicts
		var slots = await GetSlotDtos(schemaId, ct);
		var conflictList = await conflicts.DetectAsync(schemaId, ct);
		return Ok(new SlotsAndConflictsDto(slots, conflictList));
	}

	[HttpDelete("{schemaId:guid}/slots/{timeSlotId:guid}/{weekday:int}")]
	public async Task<ActionResult<SlotsAndConflictsDto>> DeleteSlot(Guid classId, Guid schemaId, Guid timeSlotId,
		int weekday, CancellationToken ct)
	{
		// Validate weekday is in valid range (0-6 for DayOfWeek enum)
		if (weekday is < 0 or > 6)
		{
			return BadRequest(new ProblemDetails
			{
				Title = "Ugyldigt ugedag",
				Detail = "Ugedagen skal være mellem 0 (søndag) og 6 (lørdag).",
				Status = 400
			});
		}

		var authResult = await authz.AuthorizeAsync(User, classId, Policies.EditClass);
		if (!authResult.Succeeded)
		{
			return Forbid();
		}

		var schemaExists = await db.Schemas.AnyAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (!schemaExists)
		{
			return NotFound();
		}

		var slot = await db.SchemaSlots.FirstOrDefaultAsync(
					   s => s.SchemaId == schemaId && s.TimeSlotId == timeSlotId && s.Weekday == (DayOfWeek)weekday,
					   ct);

		if (slot is null)
		{
			return NotFound();
		}

		db.SchemaSlots.Remove(slot);
		await db.SaveChangesAsync(ct);

		var slots = await GetSlotDtos(schemaId, ct);
		var conflictList = await conflicts.DetectAsync(schemaId, ct);
		return Ok(new SlotsAndConflictsDto(slots, conflictList));
	}

	[HttpGet("{schemaId:guid}/conflicts")]
	public async Task<ActionResult<List<ConflictInfo>>> GetConflicts(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var schemaExists = await db.Schemas.AnyAsync(s => s.Id == schemaId && s.ClassId == classId, ct);

		return schemaExists
				   ? Ok(await conflicts.DetectAsync(schemaId, ct))
				   : NotFound();
	}

	private async Task<IReadOnlyList<SlotDto>> GetSlotDtos(Guid schemaId, CancellationToken ct) =>
		await db.SchemaSlots
				.AsNoTrackingWithIdentityResolution()
				.Where(s => s.SchemaId == schemaId)
				.Include(s => s.Course)
				.Include(s => s.Teacher)
				.Include(s => s.Room)
				.Include(s => s.Aide)
				.Select(s => new SlotDto(
							s.Id,
							s.TimeSlotId,
							s.Weekday,
							s.CourseId,
							s.Course.Name,
							s.TeacherId,
							s.Teacher.Name,
							s.RoomId,
							s.Room != null ? s.Room.Name : null,
							s.AideId,
							s.Aide != null ? s.Aide.Name : null))
				.ToListAsync(ct);
}
