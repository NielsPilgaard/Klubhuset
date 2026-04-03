using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Domain;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController(AppDbContext db) : ControllerBase
{
	private static readonly HashSet<string> ReservedSlugs =
	[
		"api", "admin", "www", "static", "health", "app",
		"dashboard", "login", "logout", "signup"
	];

	public record CreateTenantRequest(
		[Required][MinLength(3)][MaxLength(40)] string Slug,
		[Required][MinLength(1)] string Name,
		string? ContactEmail);

	public record TenantDto(Guid Id, string Slug, string Name, string? ContactEmail);

	/// <summary>
	/// Resolve a slug to a tenant. Returns 404 for unknown slugs.
	/// Used by path-based routing to validate slugs before injecting TenantId.
	/// </summary>
	[HttpGet("{slug}")]
	[AllowAnonymous]
	public async Task<ActionResult<TenantDto>> GetBySlug(string slug, CancellationToken ct)
	{
		if (!IsValidSlug(slug))
		{
			return NotFound();
		}

		// Schools are not tenant-scoped themselves (they ARE the tenant root)
		var school = await db.Schools
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(s => s.Slug == slug, ct);

		return school is null
				   ? NotFound()
				   : Ok(new TenantDto(school.Id, school.Slug, school.Name, school.ContactEmail));
	}

	/// <summary>
	/// Create a new tenant (school). Called during school signup.
	/// The slug is validated and checked for global uniqueness here.
	/// </summary>
	[HttpPost]
	[AllowAnonymous]
	public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
	{
		if (!IsValidSlug(req.Slug))
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["slug"] = ["Slug må kun indeholde små bogstaver, tal og bindestreger (3–40 tegn)."] }
			});
		}

		if (ReservedSlugs.Contains(req.Slug))
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["slug"] = [$"'{req.Slug}' er reserveret og kan ikke bruges."] }
			});
		}

		var slugTaken = await db.Schools
			.IgnoreQueryFilters()
			.AnyAsync(s => s.Slug == req.Slug, ct);

		if (slugTaken)
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["slug"] = ["Dette slug er allerede taget."] }
			});
		}

		var id = Guid.NewGuid();
		var school = new School
		{
			Id = id,
			Name = req.Name,
			Slug = req.Slug,
			ContactEmail = req.ContactEmail,
		};

		db.Schools.Add(school);
		await db.SaveChangesAsync(ct);

		return CreatedAtAction(nameof(GetBySlug), new { slug = school.Slug },
			new TenantDto(school.Id, school.Slug, school.Name, school.ContactEmail));
	}

	private static bool IsValidSlug(string slug) =>
		slug.Length is >= 3 and <= 40 &&
		slug.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
}
