using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/classes")]
[Authorize]
public sealed class ClassesController(AppDbContext context, ITenantContext tenant) : ControllerBase
{
	public record ClassDto(Guid Id, string Name, string? Description, int? GradeLevel, bool IsAccessibleToCurrentUser = true);
	public record UpsertClassRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[StringLength(1000)] string? Description,
		[Range(0, 10)] int? GradeLevel);

	[HttpGet]
	public async Task<ActionResult<List<ClassDto>>> GetAll(CancellationToken ct)
	{
		var isAdmin = User.IsInRole(Roles.Admin);

		if (isAdmin)
		{
			var classes = await context.Classes
				.AsNoTracking()
				.OrderBy(c => c.Name)
				.Select(c => new ClassDto(c.Id, c.Name, c.Description, c.GradeLevel, true))
				.ToListAsync(ct);
			return Ok(classes);
		}

		// For non-admins, resolve their StaffId and filter to accessible classes
		var subject = User.GetKeycloakSubject();
		var staffId = await context.Staff
			.AsNoTracking()
			.Where(s => s.KeycloakSubject == subject)
			.Select(s => (Guid?)s.Id)
			.FirstOrDefaultAsync(ct);

		var allClasses = await context.Classes
			.AsNoTracking()
			.OrderBy(c => c.Name)
			.Select(c => new ClassDto(c.Id, c.Name, c.Description, c.GradeLevel, true))
			.ToListAsync(ct);

		// Load all permission rows for this tenant (scoped by global query filter)
		var permissionsByClass = await context.ClassPermissions
			.AsNoTracking()
			.GroupBy(p => p.ClassId)
			.Select(g => new { ClassId = g.Key, StaffIds = g.Select(p => p.StaffId).ToList() })
			.ToListAsync(ct);

		// staffId == null means no linked Staff row — treat as no access to any restricted class
		var restrictedClassIds = permissionsByClass
			.Where(g => g.StaffIds.Count > 0 && (staffId == null || !g.StaffIds.Contains(staffId.Value)))
			.Select(g => g.ClassId)
			.ToHashSet();

		var result = allClasses
			.Where(c => !restrictedClassIds.Contains(c.Id))
			.ToList();

		return Ok(result);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ClassDto>> GetById(Guid id, CancellationToken ct)
	{
		var @class = await context.Classes
								  .AsNoTracking()
								  .Where(c => c.Id == id)
								  .Select(c => new ClassDto(c.Id, c.Name, c.Description, c.GradeLevel))
								  .FirstOrDefaultAsync(ct);

		return @class is null
				   ? NotFound()
				   : Ok(@class);
	}

	[HttpPost]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<ClassDto>> Create([FromBody] UpsertClassRequest req, CancellationToken ct)
	{
		var @class = new Class
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Description = req.Description,
			GradeLevel = req.GradeLevel,
		};

		context.Classes.Add(@class);

		await context.SaveChangesAsync(ct);

		return CreatedAtAction(nameof(GetById), new { id = @class.Id },
			new ClassDto(@class.Id, @class.Name, @class.Description, @class.GradeLevel));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<ClassDto>> Update(Guid id, [FromBody] UpsertClassRequest req, CancellationToken ct)
	{
		var @class = await context.Classes.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (@class is null)
		{
			return NotFound();
		}

		@class.Name = req.Name;
		@class.Description = req.Description;
		@class.GradeLevel = req.GradeLevel;

		await context.SaveChangesAsync(ct);

		return Ok(new ClassDto(@class.Id, @class.Name, @class.Description, @class.GradeLevel));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = Roles.Admin)]
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
