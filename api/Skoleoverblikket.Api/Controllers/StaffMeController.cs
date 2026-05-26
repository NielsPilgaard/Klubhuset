using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Storage;
using System.Security.Claims;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/staff/me")]
[Authorize]
public sealed class StaffMeController(AppDbContext db, IObjectStorage storage) : ControllerBase
{
	public record AvatarPresignRequest(string ContentType, long FileSizeBytes);
	public record AvatarPresignResponse(string UploadUrl, string ObjectKey);

	[HttpPost("avatar/presign")]
	public async Task<ActionResult<AvatarPresignResponse>> PresignAvatar(
		[FromBody] AvatarPresignRequest req, CancellationToken ct)
	{
		if (!IsAllowedImageContentType(req.ContentType))
		{
			return BadRequest(new { detail = "ContentType must be image/jpeg, image/png, or image/webp." });
		}

		if (req.FileSizeBytes is <= 0 or > 5 * 1024 * 1024)
		{
			return BadRequest(new { detail = "File must be between 1 byte and 5 MB." });
		}

		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var staff = await db.Staff.AsNoTracking()
			.FirstOrDefaultAsync(s => s.KeycloakSubject == subject, ct);

		if (staff is null)
		{
			return NotFound();
		}

		var ext = ContentTypeToExtension(req.ContentType);
		var key = $"avatars/{staff.TenantId}/staff/{staff.Id}{ext}";
		var expiry = TimeSpan.FromMinutes(15);

		var (uploadUrl, _) = await storage.GeneratePresignedUploadUrlAsync(
			key, req.ContentType, req.FileSizeBytes, expiry, ct);

		return Ok(new AvatarPresignResponse(uploadUrl, key));
	}

	public record AvatarConfirmRequest(string ObjectKey);

	[HttpPost("avatar/confirm")]
	public async Task<IActionResult> ConfirmAvatar(
		[FromBody] AvatarConfirmRequest req, CancellationToken ct)
	{
		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var staff = await db.Staff
			.FirstOrDefaultAsync(s => s.KeycloakSubject == subject, ct);

		if (staff is null)
		{
			return NotFound();
		}

		var expectedPrefix = $"avatars/{staff.TenantId}/staff/{staff.Id}";
		if (!req.ObjectKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
		{
			return BadRequest(new { detail = "Invalid object key." });
		}

		var (_, publicUrl) = await storage.GeneratePresignedUploadUrlAsync(
			req.ObjectKey, "image/jpeg", 1, TimeSpan.FromHours(24 * 365), ct);

		staff.AvatarUrl = publicUrl;
		await db.SaveChangesAsync(ct);

		return NoContent();
	}

	private static bool IsAllowedImageContentType(string contentType) =>
		contentType is "image/jpeg" or "image/png" or "image/webp";

	private static string ContentTypeToExtension(string contentType) => contentType switch
	{
		"image/jpeg" => ".jpg",
		"image/png" => ".png",
		"image/webp" => ".webp",
		_ => ".bin",
	};
}
