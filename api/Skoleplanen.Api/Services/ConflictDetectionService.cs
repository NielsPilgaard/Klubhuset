using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Services;

public enum ConflictType
{
	TeacherDoubleBooked,
	RoomDoubleBooked,
	AideDoubleBooked
}

public record ConflictInfo(
	ConflictType Type,
	Guid SlotAId,
	Guid SlotBId,
	Guid ResourceId, // TeacherId / RoomId / AideId
	string ResourceName,
	DayOfWeek Weekday,
	TimeOnly StartTime,
	TimeOnly EndTime,
	string SlotACourseName,
	string SlotBCourseName,
	string SlotAClassName,
	string SlotBClassName);

public sealed class ConflictDetectionService(AppDbContext db)
{
	/// <summary>
	/// Detects all conflicts in the schema identified by <paramref name="schemaId"/>.
	/// Compares clock-time overlap across ALL active schemas in the tenant,
	/// so teacher/room/aide double-bookings across different classes are caught.
	/// </summary>
	public async Task<IReadOnlyList<ConflictInfo>> DetectAsync(Guid schemaId, CancellationToken ct = default)
	{
		// Load the target schema's slots with time information
		var targetSlots = await db.SchemaSlots
								  .Where(s => s.SchemaId == schemaId)
								  .Include(s => s.TimeSlot)
								  .Include(s => s.Course)
								  .Include(s => s.Teacher)
								  .Include(s => s.Room)
								  .Include(s => s.Aide)
								  .Include(s => s.Schema).ThenInclude(sc => sc.Class)
								  .ToListAsync(ct);

		if (targetSlots.Count == 0)
		{
			return [];
		}

		// Load all other active schema slots for the same tenant (for cross-class conflict detection)
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var otherSlots = await db.SchemaSlots
								 .Where(s => s.SchemaId != schemaId && s.Schema.StartDate <= today && s.Schema.EndDate >= today)
								 .Include(s => s.TimeSlot)
								 .Include(s => s.Course)
								 .Include(s => s.Teacher)
								 .Include(s => s.Room)
								 .Include(s => s.Aide)
								 .Include(s => s.Schema).ThenInclude(sc => sc.Class)
								 .ToListAsync(ct);

		// All slots to check against = target + other active schemas
		var allSlots = targetSlots.Concat(otherSlots).ToList();

		var conflicts = new List<ConflictInfo>();

		foreach (var a in targetSlots)
		{
			foreach (var b in allSlots)
			{
				// Don't compare a slot with itself
				if (a.Id == b.Id)
				{
					continue;
				}

				// Must be on the same weekday and have overlapping clock time
				if (a.Weekday != b.Weekday)
				{
					continue;
				}

				if (!Overlaps(a.TimeSlot.StartTime, a.TimeSlot.EndTime, b.TimeSlot.StartTime, b.TimeSlot.EndTime))
				{
					continue;
				}

				// Teacher double-booking
				if (a.TeacherId == b.TeacherId)
				{
					conflicts.Add(new ConflictInfo(
									  ConflictType.TeacherDoubleBooked,
									  a.Id,
									  b.Id,
									  a.TeacherId,
									  a.Teacher?.Name ?? string.Empty,
									  a.Weekday,
									  a.TimeSlot.StartTime,
									  a.TimeSlot.EndTime,
									  a.Course?.Name ?? string.Empty,
									  b.Course?.Name ?? string.Empty,
									  a.Schema?.Class?.Name ?? string.Empty,
									  b.Schema?.Class?.Name ?? string.Empty));
				}

				// Room double-booking
				if (a.RoomId.HasValue && a.RoomId == b.RoomId)
				{
					conflicts.Add(new ConflictInfo(
									  ConflictType.RoomDoubleBooked,
									  a.Id,
									  b.Id,
									  a.RoomId.Value,
									  a.Room?.Name ?? $"Room #{a.RoomId}",
									  a.Weekday,
									  a.TimeSlot.StartTime,
									  a.TimeSlot.EndTime,
									  a.Course?.Name ?? string.Empty,
									  b.Course?.Name ?? string.Empty,
									  a.Schema?.Class?.Name ?? string.Empty,
									  b.Schema?.Class?.Name ?? string.Empty));
				}

				// Aide double-booking
				if (a.AideId.HasValue && a.AideId == b.AideId)
				{
					conflicts.Add(new ConflictInfo(
									  ConflictType.AideDoubleBooked,
									  a.Id,
									  b.Id,
									  a.AideId.Value,
									  a.Aide?.Name ?? $"Aide #{a.AideId}",
									  a.Weekday,
									  a.TimeSlot.StartTime,
									  a.TimeSlot.EndTime,
									  a.Course?.Name ?? string.Empty,
									  b.Course?.Name ?? string.Empty,
									  a.Schema?.Class?.Name ?? string.Empty,
									  b.Schema?.Class?.Name ?? string.Empty));
				}
			}
		}

		// Deduplicate: (A,B) and (B,A) with the same ConflictType are the same conflict
		var seen = new HashSet<(Guid, Guid, ConflictType)>();
		var deduped = new List<ConflictInfo>();
		foreach (var c in conflicts)
		{
			var (slot1, slot2) = c.SlotAId.CompareTo(c.SlotBId) <= 0
									 ? (c.SlotAId, c.SlotBId)
									 : (c.SlotBId, c.SlotAId);

			if (seen.Add((slot1, slot2, c.Type)))
			{
				deduped.Add(c);
			}
		}

		return deduped;
	}

	private static bool Overlaps(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd) =>
		aStart < bEnd && bStart < aEnd;
}
