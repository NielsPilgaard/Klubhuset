using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/sfo/ugeplan")]
[Authorize(Roles = Roles.Admin)]
public sealed class SfoWeekPlanController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
	public record SfoWeekPlanShiftDto(
		Guid Id,
		Guid SfoShiftId,
		int DayOfWeek,
		string StartTime,
		string EndTime,
		string? Label,
		IReadOnlyList<SfoStaffRefDto> Staff,
		string? Beskrivelse);

	public record SfoStaffRefDto(Guid Id, string Name);

	public record SfoWeekPlanDto(
		Guid Id,
		int IsoYear,
		int IsoWeek,
		IReadOnlyList<SfoWeekPlanShiftDto> Shifts);

	public record UpsertSfoWeekPlanShiftRequest(
		[Required] int IsoYear,
		[Required] int IsoWeek,
		[Required] Guid SfoShiftId,
		[StringLength(4000)] string? Beskrivelse);

	[HttpGet]
	public async Task<ActionResult<SfoWeekPlanDto>> Get(
		[FromQuery] int? isoYear,
		[FromQuery] int? isoWeek,
		CancellationToken ct)
	{
		if (isoYear is null || isoWeek is null)
		{

			return Problem("isoYear og isoWeek er påkrævet", statusCode: 400);
		}

		if (isoYear < 2020 || isoYear > 2100 || isoWeek < 1 || isoWeek > 53)
		{

			return Problem("Ugyldigt årstal eller ugenummer", statusCode: 400);
		}

		var shifts = await db.SfoShifts
			.AsNoTracking()
			.Include(s => s.StaffAssignments).ThenInclude(ss => ss.Staff)
			.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
			.ToListAsync(ct);

		var weekPlan = await db.SfoWeekPlans
			.AsNoTracking()
			.Include(w => w.Shifts)
			.FirstOrDefaultAsync(w => w.IsoYear == isoYear.Value && w.IsoWeek == isoWeek.Value, ct);

		var weekPlanId = weekPlan?.Id ?? Guid.Empty;

		var shiftDtos = shifts.Select(shift =>
		{
			var weekShift = weekPlan?.Shifts.FirstOrDefault(ws => ws.SfoShiftId == shift.Id);
			return new SfoWeekPlanShiftDto(
				weekShift?.Id ?? Guid.Empty,
				shift.Id,
				shift.DayOfWeek,
				shift.StartTime.ToString("HH:mm"),
				shift.EndTime.ToString("HH:mm"),
				shift.Label,
				shift.StaffAssignments.Select(sa => new SfoStaffRefDto(sa.StaffId, sa.Staff.Name)).ToList(),
				weekShift?.Beskrivelse);
		}).ToList();

		return Ok(new SfoWeekPlanDto(weekPlanId, isoYear.Value, isoWeek.Value, shiftDtos));
	}

	[HttpPut("shifts")]
	public async Task<ActionResult<SfoWeekPlanShiftDto>> UpsertShift(
		[FromBody] UpsertSfoWeekPlanShiftRequest request,
		CancellationToken ct)
	{
		if (request.IsoYear < 2020 || request.IsoYear > 2100 || request.IsoWeek < 1 || request.IsoWeek > 53)
		{

			return Problem("Ugyldigt årstal eller ugenummer", statusCode: 400);
		}


		var shift = await db.SfoShifts
			.AsNoTracking()
			.Include(s => s.StaffAssignments).ThenInclude(ss => ss.Staff)
			.FirstOrDefaultAsync(s => s.Id == request.SfoShiftId, ct);

		if (shift is null)
		{

			return NotFound();
		}


		var weekPlan = await db.SfoWeekPlans
			.Include(w => w.Shifts)
			.FirstOrDefaultAsync(w => w.IsoYear == request.IsoYear && w.IsoWeek == request.IsoWeek, ct);

		if (weekPlan is null)
		{
			weekPlan = new SfoWeekPlan
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				IsoYear = request.IsoYear,
				IsoWeek = request.IsoWeek,
			};
			db.SfoWeekPlans.Add(weekPlan);
		}

		var weekShift = weekPlan.Shifts.FirstOrDefault(ws => ws.SfoShiftId == request.SfoShiftId);
		if (weekShift is null)
		{
			weekShift = new SfoWeekPlanShift
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				SfoWeekPlanId = weekPlan.Id,
				SfoShiftId = request.SfoShiftId,
			};
			weekPlan.Shifts.Add(weekShift);
		}

		weekShift.Beskrivelse = request.Beskrivelse;
		weekShift.UpdatedAt = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(ct);

		return Ok(new SfoWeekPlanShiftDto(
			weekShift.Id,
			shift.Id,
			shift.DayOfWeek,
			shift.StartTime.ToString("HH:mm"),
			shift.EndTime.ToString("HH:mm"),
			shift.Label,
			shift.StaffAssignments.Select(sa => new SfoStaffRefDto(sa.StaffId, sa.Staff.Name)).ToList(),
			weekShift.Beskrivelse));
	}
}
