using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/schools")]
[Authorize(Roles = "admin")]
public sealed class SchoolsController(AppDbContext db, ITenantContext tenant, IObjectStorage storage) : ControllerBase
{
	public record SchoolSettingsDto(string Name, string? ContactEmail, string? ContactPhone, string? LogoUrl);

	public record UpdateSchoolSettingsRequest(
		[Required][StringLength(200, MinimumLength = 1)] string Name,
		[EmailAddress] string? ContactEmail,
		[Phone] string? ContactPhone);

	[HttpGet("settings")]
	public async Task<ActionResult<SchoolSettingsDto>> GetSettings(CancellationToken ct)
	{
		var school = await db.Schools
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(s => s.Id == tenant.TenantId, ct);

		if (school is null)
		{
			return NotFound();
		}

		return Ok(new SchoolSettingsDto(school.Name, school.ContactEmail, school.ContactPhone, school.LogoUrl));
	}

	[HttpPut("settings")]
	public async Task<ActionResult<SchoolSettingsDto>> UpdateSettings(
		[FromBody] UpdateSchoolSettingsRequest req,
		CancellationToken ct)
	{
		var school = await db.Schools
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(s => s.Id == tenant.TenantId, ct);

		if (school is null)
		{
			return NotFound();
		}

		school.Name = req.Name;
		school.ContactEmail = req.ContactEmail;
		school.ContactPhone = req.ContactPhone;

		await db.SaveChangesAsync(ct);
		return Ok(new SchoolSettingsDto(school.Name, school.ContactEmail, school.ContactPhone, school.LogoUrl));
	}

	[HttpPost("logo")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<SchoolSettingsDto>> UploadLogo(IFormFile file, CancellationToken ct)
	{
		const long maxBytes = 2 * 1024 * 1024; // 2 MB
		if (file.Length > maxBytes)
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["file"] = ["Logo må maksimalt være 2 MB."] }
			});
		}

		var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (ext is not (".png" or ".jpg" or ".jpeg" or ".svg" or ".webp"))
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["file"] = ["Kun PNG, JPG, SVG og WebP er tilladt."] }
			});
		}

		var school = await db.Schools
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(s => s.Id == tenant.TenantId, ct);

		if (school is null)
		{
			return NotFound();
		}

		await using var stream = file.OpenReadStream();
		var key = $"logos/{tenant.TenantId}{ext}";
		var url = await storage.UploadAsync(key, file.ContentType, stream, ct);

		school.LogoUrl = url;
		await db.SaveChangesAsync(ct);

		return Ok(new SchoolSettingsDto(school.Name, school.ContactEmail, school.ContactPhone, school.LogoUrl));
	}
}
