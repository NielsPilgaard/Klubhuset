using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Storage;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/schools")]
[Authorize(Roles = Roles.Admin)]
public sealed class SchoolsController(AppDbContext db, ITenantContext tenant, IObjectStorage storage) : ControllerBase
{
	public record SchoolSettingsDto(string Name, string? ContactEmail, string? ContactPhone, string? LogoUrl);

	public record OnboardingStatusDto(
		bool HasLogo,
		int StaffCount,
		int ClassCount,
		int CourseCount,
		int RoomCount,
		int StepsCompleted,
		int StepsTotal);

	public record UpdateSchoolSettingsRequest(
		[Required]
		[StringLength(200, MinimumLength = 1)]
		string Name,
		[EmailAddress] string? ContactEmail,
		[Phone] string? ContactPhone);

	[HttpGet("settings")]
	public async Task<ActionResult<SchoolSettingsDto>> GetSettings(CancellationToken ct)
	{
		var school = await db.Schools
							 .AsNoTracking()
							 .IgnoreQueryFilters()
							 .Where(s => s.Id == tenant.TenantId)
							 .Select(s => new SchoolSettingsDto(s.Name, s.ContactEmail, s.ContactPhone, s.LogoUrl))
							 .FirstOrDefaultAsync(ct);

		return school is null
				   ? NotFound()
				   : Ok(school);
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

	[HttpGet("onboarding-status")]
	public async Task<ActionResult<OnboardingStatusDto>> GetOnboardingStatus(CancellationToken ct)
	{
		var school = await db.Schools
							 .AsNoTracking()
							 .IgnoreQueryFilters()
							 .FirstOrDefaultAsync(s => s.Id == tenant.TenantId, ct);

		if (school is null)
		{
			return NotFound();
		}

		var staffCount = await db.Staff.CountAsync(ct);
		var classCount = await db.Classes.CountAsync(ct);
		var courseCount = await db.Courses.CountAsync(ct);
		var roomCount = await db.Rooms.CountAsync(ct);
		var hasLogo = !string.IsNullOrEmpty(school.LogoUrl);

		var stepsCompleted =
			(hasLogo ? 1 : 0) +
			(staffCount > 0 ? 1 : 0) +
			(classCount > 0 ? 1 : 0) +
			(courseCount > 0 ? 1 : 0) +
			(roomCount > 0 ? 1 : 0);

		return Ok(new OnboardingStatusDto(hasLogo,
										  staffCount,
										  classCount,
										  courseCount,
										  roomCount,
										  stepsCompleted,
										  StepsTotal: 5));
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

		// Map extension to safe MIME type; reject unknown extensions
		var mimeType = ext switch
		{
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".webp" => "image/webp",
			_ => null
		};

		if (mimeType is null)
		{
			return ValidationProblem(new ValidationProblemDetails
			{
				Errors = { ["file"] = ["Kun PNG, JPG og WebP er tilladt."] }
			});
		}

		var school = await db.Schools
							 .IgnoreQueryFilters()
							 .FirstOrDefaultAsync(s => s.Id == tenant.TenantId, ct);

		if (school is null)
		{
			return NotFound();
		}

		if (school.LogoUrl is not null)
		{
			var oldKey = storage.GetKeyFromPublicUrl(school.LogoUrl);
			if (oldKey is not null)
			{
				try { await storage.DeleteAsync(oldKey, ct); }
				catch { /* swallow — old file missing or inaccessible should not block upload */ }
			}
		}

		await using var stream = file.OpenReadStream();
		var key = $"logos/{tenant.TenantId}{ext}";
		var url = await storage.UploadPublicAsync(key, mimeType, stream, ct);

		school.LogoUrl = url;
		await db.SaveChangesAsync(ct);

		return Ok(new SchoolSettingsDto(school.Name, school.ContactEmail, school.ContactPhone, school.LogoUrl));
	}
}
