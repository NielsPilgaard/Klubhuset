using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize]
public sealed class StaffController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
	public record StaffDto(Guid Id, string Name, string? Email, string? Phone, StaffRole Role);
	public record UpsertStaffRequest(string Name, string? Email, string? Phone, StaffRole Role);

	[HttpGet]
	public async Task<ActionResult<List<StaffDto>>> GetAll(CancellationToken ct)
	{
		var staff = await db.Staff
			.AsNoTracking()
			.OrderBy(s => s.Name)
			.Select(s => new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role))
			.ToListAsync(ct);
		return Ok(staff);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<StaffDto>> GetById(Guid id, CancellationToken ct)
	{
		var s = await db.Staff
			.AsNoTracking()
			.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (s is null)
		{
			return NotFound();
		}

		return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role));
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<StaffDto>> Create([FromBody] UpsertStaffRequest req, CancellationToken ct)
	{
		var s = new Staff
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Email = req.Email,
			Phone = req.Phone,
			Role = req.Role,
		};
		db.Staff.Add(s);
		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById), new { id = s.Id },
			new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<StaffDto>> Update(Guid id, [FromBody] UpsertStaffRequest req, CancellationToken ct)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (s is null)
		{
			return NotFound();
		}

		s.Name = req.Name;
		s.Email = req.Email;
		s.Phone = req.Phone;
		s.Role = req.Role;
		await db.SaveChangesAsync(ct);
		return Ok(new StaffDto(s.Id, s.Name, s.Email, s.Phone, s.Role));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var s = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (s is null)
		{
			return NotFound();
		}

		db.Staff.Remove(s);
		await db.SaveChangesAsync(ct);
		return NoContent();
	}
}
