using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/sfo/shifts")]
[Authorize(Roles = Roles.Admin)]
public sealed class SfoController(AppDbContext context, ITenantContext tenant) : ControllerBase
{
	public record StaffRefDto(Guid Id, string Name);
	public record SfoShiftDto(Guid Id, int DayOfWeek, string StartTime, string EndTime, string? Label, IReadOnlyList<StaffRefDto> Staff);
	public record UpsertSfoShiftRequest(
		[Range(1, 5)] int DayOfWeek,
		[Required] string StartTime,
		[Required] string EndTime,
		[StringLength(200)] string? Label);

	[HttpGet]
	public async Task<ActionResult<List<SfoShiftDto>>> GetAll(CancellationToken ct)
	{
		var shifts = await context.SfoShifts
			.AsNoTracking()
			.Include(s => s.StaffAssignments)
				.ThenInclude(sa => sa.Staff)
			.OrderBy(s => s.DayOfWeek)
			.ThenBy(s => s.StartTime)
			.ToListAsync(ct);

		return Ok(shifts.Select(ToDto).ToList());
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<SfoShiftDto>> GetById(Guid id, CancellationToken ct)
	{
		var shift = await context.SfoShifts
			.AsNoTracking()
			.Include(s => s.StaffAssignments)
				.ThenInclude(sa => sa.Staff)
			.FirstOrDefaultAsync(s => s.Id == id, ct);

		return shift is null ? NotFound() : Ok(ToDto(shift));
	}

	[HttpPost]
	public async Task<ActionResult<SfoShiftDto>> Create([FromBody] UpsertSfoShiftRequest req, CancellationToken ct)
	{
		if (!TryParseTime(req.StartTime, out var start) || !TryParseTime(req.EndTime, out var end))
		{

			return ValidationProblem("StartTime and EndTime must be HH:mm.");
		}

		if (end <= start)
		{

			return ValidationProblem("EndTime must be after StartTime.");
		}

		var shift = new SfoShift
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			DayOfWeek = req.DayOfWeek,
			StartTime = start,
			EndTime = end,
			Label = req.Label,
		};

		context.SfoShifts.Add(shift);
		await context.SaveChangesAsync(ct);

		return CreatedAtAction(nameof(GetById), new { id = shift.Id }, ToDto(shift));
	}

	[HttpPut("{id:guid}")]
	public async Task<ActionResult<SfoShiftDto>> Update(Guid id, [FromBody] UpsertSfoShiftRequest req, CancellationToken ct)
	{
		if (!TryParseTime(req.StartTime, out var start) || !TryParseTime(req.EndTime, out var end))
		{

			return ValidationProblem("StartTime and EndTime must be HH:mm.");
		}

		if (end <= start)
		{

			return ValidationProblem("EndTime must be after StartTime.");
		}

		var shift = await context.SfoShifts
			.Include(s => s.StaffAssignments)
				.ThenInclude(sa => sa.Staff)
			.FirstOrDefaultAsync(s => s.Id == id, ct);

		if (shift is null)
		{
			return NotFound();
		}

		shift.DayOfWeek = req.DayOfWeek;
		shift.StartTime = start;
		shift.EndTime = end;
		shift.Label = req.Label;

		await context.SaveChangesAsync(ct);

		return Ok(ToDto(shift));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var shift = await context.SfoShifts.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (shift is null)
		{
			return NotFound();
		}

		context.SfoShifts.Remove(shift);
		await context.SaveChangesAsync(ct);

		return NoContent();
	}

	[HttpPost("{id:guid}/staff/{staffId:guid}")]
	public async Task<ActionResult> AssignStaff(Guid id, Guid staffId, CancellationToken ct)
	{
		var shift = await context.SfoShifts.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (shift is null)
		{
			return NotFound();
		}

		var staffExists = await context.Staff.AnyAsync(s => s.Id == staffId, ct);
		if (!staffExists)
		{
			return NotFound();
		}

		var already = await context.SfoShiftStaff.AnyAsync(ss => ss.ShiftId == id && ss.StaffId == staffId, ct);
		if (already)
		{
			return Conflict();
		}

		context.SfoShiftStaff.Add(new SfoShiftStaff
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ShiftId = id,
			StaffId = staffId,
		});

		await context.SaveChangesAsync(ct);
		return NoContent();
	}

	[HttpDelete("{id:guid}/staff/{staffId:guid}")]
	public async Task<ActionResult> RemoveStaff(Guid id, Guid staffId, CancellationToken ct)
	{
		var assignment = await context.SfoShiftStaff
			.FirstOrDefaultAsync(ss => ss.ShiftId == id && ss.StaffId == staffId, ct);

		if (assignment is null)
		{
			return NotFound();
		}

		context.SfoShiftStaff.Remove(assignment);
		await context.SaveChangesAsync(ct);
		return NoContent();
	}

	private static SfoShiftDto ToDto(SfoShift s) => new(
		s.Id,
		s.DayOfWeek,
		s.StartTime.ToString("HH:mm"),
		s.EndTime.ToString("HH:mm"),
		s.Label,
		s.StaffAssignments.Select(sa => new StaffRefDto(sa.StaffId, sa.Staff?.Name ?? "")).ToList()
	);

	private static bool TryParseTime(string value, out TimeOnly result)
		=> TimeOnly.TryParseExact(value, "HH:mm", out result);
}
