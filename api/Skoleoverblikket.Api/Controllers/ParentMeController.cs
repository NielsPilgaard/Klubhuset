using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using System.Security.Claims;

namespace Skoleoverblikket.Api.Controllers;

/// <summary>
/// Self-service endpoints for authenticated parents.
/// </summary>
[ApiController]
[Route("api/v1/parents/me")]
[Authorize(Roles = Roles.Parent)]
public sealed class ParentMeController(AppDbContext db) : ControllerBase
{
	public record ParentMeDto(Guid Id, string Name, IReadOnlyList<ParentClassDto> Classes);
	public record ParentClassDto(Guid ClassId, string ClassName);

	[HttpGet]
	public async Task<ActionResult<ParentMeDto>> GetMe(CancellationToken ct)
	{
		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub");

		if (subject is null)
		{
			return Unauthorized();
		}

		var parent = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students).ThenInclude(s => s.Class)
			.FirstOrDefaultAsync(p => p.KeycloakSubject == subject, ct);

		if (parent is null)
		{
			return NotFound();
		}

		var classes = parent.Students
			.Select(s => new ParentClassDto(s.ClassId, s.Class?.Name ?? string.Empty))
			.DistinctBy(c => c.ClassId)
			.ToList();

		return Ok(new ParentMeDto(parent.Id, parent.Name, classes));
	}
}
