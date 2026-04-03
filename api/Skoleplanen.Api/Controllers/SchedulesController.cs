using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class SchedulesController(AppDbContext db) : ControllerBase
{
	public record ScheduleSlotDto(
		int Weekday,
		string StartTime,
		string EndTime,
		string CourseName,
		string ClassName,
		Guid? RoomId,
		string? RoomName,
		Guid? AideId,
		string? AideName,
		Guid? TeacherId,
		string? TeacherName);

	/// <summary>
	/// Weekly schedule for a staff member (teacher or aide) across all active schemas.
	/// </summary>
	[HttpGet("staff/{staffId:guid}/schedule")]
	public async Task<ActionResult<List<ScheduleSlotDto>>> GetStaffSchedule(Guid staffId, CancellationToken ct)
	{
		var staffExists = await db.Staff.AnyAsync(s => s.Id == staffId, ct);
		if (!staffExists)
		{
			return NotFound();
		}

		var slots = await db.SchemaSlots
			.AsNoTrackingWithIdentityResolution()
			.Where(s => s.Schema.IsActive && (s.TeacherId == staffId || s.AideId == staffId))
			.Include(s => s.TimeSlot)
			.Include(s => s.Course)
			.Include(s => s.Schema).ThenInclude(sc => sc.Class)
			.Include(s => s.Room)
			.Include(s => s.Teacher)
			.Include(s => s.Aide)
			.OrderBy(s => s.Weekday)
			.ThenBy(s => s.TimeSlot.SortOrder)
			.Select(s => new ScheduleSlotDto(
				s.Weekday,
				s.TimeSlot.StartTime.ToString("HH:mm"),
				s.TimeSlot.EndTime.ToString("HH:mm"),
				s.Course.Name,
				s.Schema.Class.Name,
				s.RoomId, s.Room != null ? s.Room.Name : null,
				s.AideId, s.Aide != null ? s.Aide.Name : null,
				s.TeacherId, s.Teacher.Name))
			.ToListAsync(ct);

		return Ok(slots);
	}

	/// <summary>
	/// Weekly schedule for a room across all active schemas.
	/// </summary>
	[HttpGet("rooms/{roomId:guid}/schedule")]
	public async Task<ActionResult<List<ScheduleSlotDto>>> GetRoomSchedule(Guid roomId, CancellationToken ct)
	{
		var roomExists = await db.Rooms.AnyAsync(r => r.Id == roomId, ct);
		if (!roomExists)
		{
			return NotFound();
		}

		var slots = await db.SchemaSlots
			.AsNoTrackingWithIdentityResolution()
			.Where(s => s.Schema.IsActive && s.RoomId == roomId)
			.Include(s => s.TimeSlot)
			.Include(s => s.Course)
			.Include(s => s.Schema).ThenInclude(sc => sc.Class)
			.Include(s => s.Room)
			.Include(s => s.Teacher)
			.Include(s => s.Aide)
			.OrderBy(s => s.Weekday)
			.ThenBy(s => s.TimeSlot.SortOrder)
			.Select(s => new ScheduleSlotDto(
				s.Weekday,
				s.TimeSlot.StartTime.ToString("HH:mm"),
				s.TimeSlot.EndTime.ToString("HH:mm"),
				s.Course.Name,
				s.Schema.Class.Name,
				s.RoomId, s.Room != null ? s.Room.Name : null,
				s.AideId, s.Aide != null ? s.Aide.Name : null,
				s.TeacherId, s.Teacher.Name))
			.ToListAsync(ct);

		return Ok(slots);
	}
}
