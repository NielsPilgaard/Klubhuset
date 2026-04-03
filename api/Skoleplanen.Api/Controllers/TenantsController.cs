using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;

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
		[Required]
		[MinLength(3)]
		[MaxLength(40)]
		string Slug,
		[Required]
		[MinLength(1)]
		[MaxLength(100)]
		string Name,
		[EmailAddress] string? ContactEmail);

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
	[Authorize]
	public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
	{
		if (!IsValidSlug(req.Slug))
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors =
				{
					["slug"] =
					[
						"Slug må kun indeholde små bogstaver, tal og bindestreger (3–40 tegn) og må ikke starte, slutte med eller indeholde dobbelte bindestreger."
					]
				}
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
		try
		{
			await db.SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
		{
			// Handle unique constraint violation from concurrent requests
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["slug"] = ["Dette slug er allerede taget. Prøv venligst et andet slug."] }
			});
		}

		return CreatedAtAction(nameof(GetBySlug),
							   new { slug = school.Slug },
							   new TenantDto(school.Id, school.Slug, school.Name, school.ContactEmail));
	}

	private static bool IsValidSlug(string slug) =>
		!string.IsNullOrEmpty(slug) &&
		slug.Length is >= 3 and <= 40 &&
		slug[0] != '-' &&
		slug[^1] != '-' &&
		!slug.Contains("--") &&
		slug.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

	// TODO: Use EFCore Exceptions instead
	private static bool IsUniqueConstraintViolation(DbUpdateException ex)
	{
		// Check for PostgreSQL unique constraint violation (error code 23505)
		if (ex.InnerException is Npgsql.PostgresException pgEx)
		{
			return pgEx.SqlState == "23505";
		}

		// Fallback to message-based check for other databases or if inner exception details are unavailable
		if (ex.InnerException != null)
		{
			var message = ex.InnerException.Message;
			return message.Contains("Slug", StringComparison.OrdinalIgnoreCase) ||
				   message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
				   message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}
}
