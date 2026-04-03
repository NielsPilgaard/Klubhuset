using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/stats")]
[Authorize]
public sealed class StatsController(AppDbContext db) : ControllerBase
{
	public record DashboardStats(
		int ClassCount,
		int StaffCount,
		int CourseCount,
		int RoomCount,
		int SchemasComplete,
		int SchemasTotal,
		IReadOnlyList<HoursPerCourse> HoursPerCourse,
		IReadOnlyList<HoursPerStaff> HoursPerStaff,
		IReadOnlyList<UnassignedClass> UnassignedClasses);

	public record HoursPerCourse(Guid CourseId, string CourseName, Guid ClassId, string ClassName, double Hours);
	public record HoursPerStaff(Guid StaffId, string StaffName, StaffRole Role, double Hours);
	public record UnassignedClass(Guid ClassId, string ClassName, int EmptySlots);

	[HttpGet("dashboard")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<DashboardStats>> GetDashboard(CancellationToken ct)
	{
		var classCount = await db.Classes.CountAsync(ct);
		var staffCount = await db.Staff.CountAsync(ct);
		var courseCount = await db.Courses.CountAsync(ct);
		var roomCount = await db.Rooms.CountAsync(ct);

		var allSchemas = await db.Schemas.AsNoTracking().ToListAsync(ct);
		var schemasTotal = allSchemas.Count;
		var schemasComplete = allSchemas.Count(s => s.Status == SchemaStatus.Complete);

		// Hours per course per class (active schemas only)
		var activeSlots = await db.SchemaSlots
			.AsNoTrackingWithIdentityResolution()
			.Where(s => s.Schema.IsActive)
			.Include(s => s.Course)
			.Include(s => s.Schema).ThenInclude(sc => sc.Class)
			.Include(s => s.TimeSlot)
			.Include(s => s.Teacher)
			.Include(s => s.Aide)
			.ToListAsync(ct);

		var hoursPerCourse = activeSlots
			.GroupBy(s => (s.CourseId, CourseName: s.Course.Name, s.Schema.ClassId, ClassName: s.Schema.Class.Name))
			.Select(g => new HoursPerCourse(
				g.Key.CourseId, g.Key.CourseName,
				g.Key.ClassId, g.Key.ClassName,
				Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)))
			.OrderBy(h => h.ClassName).ThenBy(h => h.CourseName)
			.ToList();

		// Hours per staff member (active schemas only)
		var teacherHours = activeSlots
			.GroupBy(s => (s.TeacherId, s.Teacher.Name, s.Teacher.Role))
			.Select(g => new HoursPerStaff(
				g.Key.TeacherId, g.Key.Name, g.Key.Role,
				Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)));

		var aideHours = activeSlots
			.Where(s => s.AideId.HasValue)
			.GroupBy(s => (AideId: s.AideId!.Value, s.Aide!.Name, s.Aide.Role))
			.Select(g => new HoursPerStaff(
				g.Key.AideId, g.Key.Name, g.Key.Role,
				Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)));

		var hoursPerStaff = teacherHours.Concat(aideHours)
			.OrderBy(h => h.StaffName)
			.ToList();

		// Unassigned slots: classes with active schemas that have time slots without assignments
		var activeSchemas = await db.Schemas
			.AsNoTrackingWithIdentityResolution()
			.Where(s => s.IsActive)
			.Include(s => s.Class)
			.Include(s => s.Slots)
			.ToListAsync(ct);

		var classTimeSlots = await db.TimeSlots.AsNoTracking().ToListAsync(ct);

		var unassigned = activeSchemas.Select(schema =>
		{
			// Time slots applicable to this class
			var applicable = classTimeSlots
				.Where(ts => ts.ClassId == schema.ClassId || ts.ClassId == null)
				.ToList();
			// Count weekday × timeslot combinations with no assignment
			var emptySlots = applicable.Count * 5 - schema.Slots.Count;
			return new UnassignedClass(schema.ClassId, schema.Class.Name, Math.Max(0, emptySlots));
		})
		.Where(u => u.EmptySlots > 0)
		.OrderByDescending(u => u.EmptySlots)
		.ToList();

		return Ok(new DashboardStats(
			classCount, staffCount, courseCount, roomCount,
			schemasComplete, schemasTotal,
			hoursPerCourse, hoursPerStaff, unassigned));
	}
}
