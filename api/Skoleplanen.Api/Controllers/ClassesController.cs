using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/classes")]
[Authorize]
public sealed class ClassesController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
	public record ClassDto(Guid Id, string Name, string? Description);
	public record UpsertClassRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[StringLength(1000)] string? Description);

	[HttpGet]
	public async Task<ActionResult<List<ClassDto>>> GetAll(CancellationToken ct)
	{
		var classes = await db.Classes
			.AsNoTracking()
			.OrderBy(c => c.Name)
			.Select(c => new ClassDto(c.Id, c.Name, c.Description))
			.ToListAsync(ct);
		return Ok(classes);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ClassDto>> GetById(Guid id, CancellationToken ct)
	{
		var c = await db.Classes
			.AsNoTracking()
			.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (c is null)
		{
			return NotFound();
		}

		return Ok(new ClassDto(c.Id, c.Name, c.Description));
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<ClassDto>> Create([FromBody] UpsertClassRequest req, CancellationToken ct)
	{
		var c = new Class
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Description = req.Description,
		};
		db.Classes.Add(c);
		await db.SaveChangesAsync(ct);
		return CreatedAtAction(nameof(GetById), new { id = c.Id },
			new ClassDto(c.Id, c.Name, c.Description));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<ClassDto>> Update(Guid id, [FromBody] UpsertClassRequest req, CancellationToken ct)
	{
		var c = await db.Classes.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (c is null)
		{
			return NotFound();
		}

		c.Name = req.Name;
		c.Description = req.Description;
		await db.SaveChangesAsync(ct);
		return Ok(new ClassDto(c.Id, c.Name, c.Description));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var c = await db.Classes.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (c is null)
		{
			return NotFound();
		}

		db.Classes.Remove(c);
		await db.SaveChangesAsync(ct);
		return NoContent();
	}
}
