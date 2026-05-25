using System.ComponentModel.DataAnnotations;
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

			return Problem("årstal eller ugenummer er påkrævet", statusCode: 400);
		}

		if (!IsoWeekValidation.IsValid(isoYear.Value, isoWeek.Value))
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
		if (!IsoWeekValidation.IsValid(request.IsoYear, request.IsoWeek))
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

		SfoWeekPlanShift weekShift;
		const int maxRetries = 3;
		for (var attempt = 0; ; attempt++)
		{
			db.ChangeTracker.Clear();

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

			weekShift = weekPlan.Shifts.FirstOrDefault(ws => ws.SfoShiftId == request.SfoShiftId)
				?? new SfoWeekPlanShift
				{
					Id = Guid.NewGuid(),
					TenantId = tenant.TenantId,
					SfoWeekPlanId = weekPlan.Id,
					SfoShiftId = request.SfoShiftId,
				};

			if (!weekPlan.Shifts.Contains(weekShift))
				weekPlan.Shifts.Add(weekShift);

			weekShift.Beskrivelse = request.Beskrivelse;
			weekShift.UpdatedAt = DateTimeOffset.UtcNow;

			try
			{
				await db.SaveChangesAsync(ct);
				break;
			}
			catch (DbUpdateException) when (attempt < maxRetries)
			{
			}
		}

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
