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
public sealed class ClassesController(AppDbContext context, ITenantContext tenant) : ControllerBase
{
	public record ClassDto(Guid Id, string Name, string? Description);
	public record UpsertClassRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[StringLength(1000)] string? Description);

	[HttpGet]
	public async Task<ActionResult<List<ClassDto>>> GetAll(CancellationToken ct)
	{
		var classes = await context.Classes
			.AsNoTracking()
			.OrderBy(c => c.Name)
			.Select(c => new ClassDto(c.Id, c.Name, c.Description))
			.ToListAsync(ct);

		return Ok(classes);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ClassDto>> GetById(Guid id, CancellationToken ct)
	{
		var @class = await context.Classes
								  .AsNoTracking()
								  .FirstOrDefaultAsync(c => c.Id == id, ct);

		return @class is null
				   ? NotFound()
				   : Ok(new ClassDto(@class.Id, @class.Name, @class.Description));
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<ClassDto>> Create([FromBody] UpsertClassRequest req, CancellationToken ct)
	{
		var @class = new Class
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Description = req.Description,
		};

		context.Classes.Add(@class);

		await context.SaveChangesAsync(ct);

		return CreatedAtAction(nameof(GetById), new { id = @class.Id },
			new ClassDto(@class.Id, @class.Name, @class.Description));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<ClassDto>> Update(Guid id, [FromBody] UpsertClassRequest req, CancellationToken ct)
	{
		var @class = await context.Classes.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (@class is null)
		{
			return NotFound();
		}

		@class.Name = req.Name;
		@class.Description = req.Description;

		await context.SaveChangesAsync(ct);

		return Ok(new ClassDto(@class.Id, @class.Name, @class.Description));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var @class = await context.Classes.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (@class is null)
		{
			return NotFound();
		}

		context.Classes.Remove(@class);

		await context.SaveChangesAsync(ct);

		return NoContent();
	}
}
