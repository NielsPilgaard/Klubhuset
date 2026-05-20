using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;
using ZiggyCreatures.Caching.Fusion;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/classes")]
[Authorize]
public sealed class ClassesController(AppDbContext context, ITenantContext tenant, IFusionCache cache) : ControllerBase
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
				.Select(c => new ClassDto(c.Id, c.Name, c.Description, c.GradeLevel, true))
				.ToListAsync(ct);
			return Ok(classes.OrderBy(c => c.Name, NaturalSortComparer.Instance).ToList());
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
			.OrderBy(c => c.Name, NaturalSortComparer.Instance)
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
		await cache.RemoveAsync(SchoolsController.OnboardingCacheKey(tenant.TenantId), token: ct);

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
		await cache.RemoveAsync(SchoolsController.OnboardingCacheKey(tenant.TenantId), token: ct);

		return NoContent();
	}

	[HttpGet("archived")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<List<ClassDto>>> GetArchived(CancellationToken ct)
	{
		var classes = await context.Classes
			.IgnoreQueryFilters()
			.Where(c => c.TenantId == tenant.TenantId && c.ArchivedAt != null)
			.Select(c => new ClassDto(c.Id, c.Name, c.Description, c.GradeLevel))
			.ToListAsync(ct);

		return Ok(classes.OrderBy(c => c.Name, NaturalSortComparer.Instance).ToList());
	}

	public record YearRollRenameEntry(Guid ClassId, string NewName);
	public record YearRollCreateEntry(string Name);
	public record YearRollRequest(
		IReadOnlyList<YearRollRenameEntry> Renames,
		IReadOnlyList<Guid> Archive,
		IReadOnlyList<YearRollCreateEntry> Create);

	[HttpPost("year-roll")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult> YearRoll([FromBody] YearRollRequest req, CancellationToken ct)
	{
		var archiveSet = req.Archive.ToHashSet();
		var renameIds = req.Renames.Select(r => r.ClassId).ToList();

		if (renameIds.Any(id => archiveSet.Contains(id)))
		{
			return ValidationProblem("A class cannot be both renamed and archived in the same year-roll.");
		}

		var renameTargets = req.Renames.Select(r => r.NewName.Trim().ToLowerInvariant()).ToList();
		if (renameTargets.Count != renameTargets.Distinct().Count())
		{
			return ValidationProblem("Two or more classes would receive the same new name.");
		}

		var affectedIds = renameIds
			.Concat(req.Archive)
			.Distinct()
			.ToList();

		var classes = await context.Classes
			.Where(c => affectedIds.Contains(c.Id))
			.ToListAsync(ct);

		foreach (var rename in req.Renames)
		{
			var @class = classes.FirstOrDefault(c => c.Id == rename.ClassId);
			if (@class is not null)
			{
				@class.Name = rename.NewName.Trim();
			}
		}

		foreach (var archiveId in req.Archive)
		{
			var @class = classes.FirstOrDefault(c => c.Id == archiveId);
			if (@class is not null)
			{
				@class.ArchivedAt = DateTimeOffset.UtcNow;
			}
		}

		foreach (var entry in req.Create)
		{
			context.Classes.Add(new Class
			{
				Id = Guid.NewGuid(),
				TenantId = tenant.TenantId,
				Name = entry.Name,
			});
		}

		await context.SaveChangesAsync(ct);
		await cache.RemoveAsync(SchoolsController.OnboardingCacheKey(tenant.TenantId), token: ct);

		return NoContent();
	}
}

// Sorts "0.a" < "1.a" < "2.a" < "10.a" by splitting leading digits from the rest.
file sealed class NaturalSortComparer : IComparer<string>
{
	public static readonly NaturalSortComparer Instance = new();

	public int Compare(string? x, string? y)
	{
		if (ReferenceEquals(x, y))
		{
			return 0;
		}

		if (x is null)
		{
			return -1;
		}

		if (y is null)
		{
			return 1;
		}

		var i = 0;
		var j = 0;
		while (i < x.Length && j < y.Length)
		{
			if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
			{
				var numStart1 = i;
				var numStart2 = j;
				while (i < x.Length && char.IsDigit(x[i]))
				{
					i++;
				}

				while (j < y.Length && char.IsDigit(y[j]))
				{
					j++;
				}

				var n1 = int.Parse(x.AsSpan(numStart1, i - numStart1));
				var n2 = int.Parse(y.AsSpan(numStart2, j - numStart2));
				var cmp = n1.CompareTo(n2);
				if (cmp != 0)
				{
					return cmp;
				}
			}
			else
			{
				var cmp = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
				if (cmp != 0)
				{
					return cmp;
				}

				i++; j++;
			}
		}

		return x.Length.CompareTo(y.Length);
	}
}
