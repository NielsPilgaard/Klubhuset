using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Services;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/classes/{classId:guid}/schemas")]
[Authorize]
public sealed class SchemasController(AppDbContext db, ITenantContext tenant, ConflictDetectionService conflicts)
	: ControllerBase
{
	public record SchemaDto(Guid Id, Guid ClassId, string Name, SchemaStatus Status, bool IsActive);

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

	public record CreateSchemaRequest([Required] [MinLength(1)] string Name);

	public record CopySchemaRequest([Required] [MinLength(1)] string Name);

	public record UpsertSlotRequest(
		[Required] Guid TimeSlotId,
		DayOfWeek Weekday,
		[Required] Guid CourseId,
		[Required] Guid TeacherId,
		Guid? RoomId,
		Guid? AideId);

	[HttpGet]
	public async Task<ActionResult<List<SchemaDto>>> GetAll(Guid classId, CancellationToken ct)
	{
		var schemas = await db.Schemas
							  .AsNoTracking()
							  .Where(s => s.ClassId == classId)
							  .OrderByDescending(s => s.CreatedAt)
							  .Select(s => new SchemaDto(s.Id, s.ClassId, s.Name, s.Status, s.IsActive))
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
					  new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.Status, schema.IsActive),
					  slots,
					  conflictList));
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<SchemaDto>> Create(Guid classId, [FromBody] CreateSchemaRequest req,
		CancellationToken ct)
	{
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
		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById),
							   new { classId, schemaId = schema.Id },
							   new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.Status, schema.IsActive));
	}

	[HttpPost("{schemaId:guid}/activate")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<SchemaDto>> Activate(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		// Deactivate all other schemas for this class
		var others = await db.Schemas.Where(s => s.ClassId == classId && s.Id != schemaId).ToListAsync(ct);
		foreach (var o in others)
		{
			o.IsActive = false;
		}

		schema.IsActive = true;

		await db.SaveChangesAsync(ct);
		return Ok(new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.Status, schema.IsActive));
	}

	[HttpPost("{schemaId:guid}/complete")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<SchemaDto>> MarkComplete(Guid classId, Guid schemaId, CancellationToken ct)
	{
		var schema = await db.Schemas.FirstOrDefaultAsync(s => s.Id == schemaId && s.ClassId == classId, ct);
		if (schema is null)
		{
			return NotFound();
		}

		var conflictList = await conflicts.DetectAsync(schemaId, ct);
		if (conflictList.Count > 0)
		{
			return Problem(
				title: "Schema har konflikter",
				detail: $"Skemaet har {conflictList.Count} konflikt(er) og kan ikke markeres som færdigt.",
				statusCode: 422);
		}

		schema.Status = SchemaStatus.Complete;
		await db.SaveChangesAsync(ct);
		return Ok(new SchemaDto(schema.Id, schema.ClassId, schema.Name, schema.Status, schema.IsActive));
	}

	[HttpPost("{schemaId:guid}/copy")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<SchemaDto>> Copy(Guid classId, Guid schemaId,
		[FromBody] CopySchemaRequest req, CancellationToken ct)
	{
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

		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById),
							   new { classId, schemaId = copy.Id },
							   new SchemaDto(copy.Id, copy.ClassId, copy.Name, copy.Status, copy.IsActive));
	}

	[HttpDelete("{schemaId:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult> Delete(Guid classId, Guid schemaId, CancellationToken ct)
	{
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
		if (!schemaExists)
		{
			return NotFound();
		}

		return Ok(await GetSlotDtos(schemaId, ct));
	}

	[HttpPut("{schemaId:guid}/slots")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<SlotsAndConflictsDto>> UpsertSlot(Guid classId, Guid schemaId,
		[FromBody] UpsertSlotRequest req, CancellationToken ct)
	{
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

		// Reset to draft when modified
		schema.Status = SchemaStatus.Draft;
		await db.SaveChangesAsync(ct);

		// Return updated slot list + conflicts
		var slots = await GetSlotDtos(schemaId, ct);
		var conflictList = await conflicts.DetectAsync(schemaId, ct);
		return Ok(new SlotsAndConflictsDto(slots, conflictList));
	}

	[HttpDelete("{schemaId:guid}/slots/{timeSlotId:guid}/{weekday:int}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<SlotsAndConflictsDto>> DeleteSlot(Guid classId, Guid schemaId, Guid timeSlotId,
		int weekday, CancellationToken ct)
	{
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
		if (!schemaExists)
		{
			return NotFound();
		}

		return Ok(await conflicts.DetectAsync(schemaId, ct));
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
