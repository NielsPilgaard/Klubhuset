using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Storage;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public sealed class FilesController(AppDbContext db, ITenantContext tenant, IObjectStorage storage, IHttpContextAccessor http) : ControllerBase
{
    // 100 GB for Basis tier
    private const long QuotaBytes = 100L * 1024 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".webp", ".txt", ".zip"];
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB per file

    public record FileDto(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        string Url,
        Guid? CourseId,
        string? CourseName,
        string UploadedBy,
        DateTimeOffset UploadedAt);

    [HttpGet]
    public async Task<ActionResult<List<FileDto>>> GetAll([FromQuery] Guid? courseId, CancellationToken ct)
    {
        var query = db.SchoolFiles
            .AsNoTracking()
            .Include(f => f.Course)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(f => f.CourseId == courseId.Value);
        }

        var files = await query
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new FileDto(
                f.Id,
                f.FileName,
                f.ContentType,
                f.SizeBytes,
                f.Url,
                f.CourseId,
                f.Course != null ? f.Course.Name : null,
                f.UploadedBy,
                f.UploadedAt))
            .ToListAsync(ct);

        return Ok(files);
    }

    // TODO: Use presigned url flow for uploads instead of uploading through the API, to avoid tying up API resources and hitting timeouts on large files. 
    // This also allows for better progress reporting on the frontend.
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FileDto>> Upload(
        IFormFile file,
        [FromForm] Guid? courseId,
        CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["file"] = ["Filen er tom."] }
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["file"] = ["Filen må maksimalt være 100 MB."] }
            });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Errors = { ["file"] = [$"Filtypen '{ext}' er ikke tilladt."] }
            });
        }

        // Quota check
        var usedBytes = await db.SchoolFiles.SumAsync(f => (long?)f.SizeBytes ?? 0, ct);
        if (usedBytes + file.Length > QuotaBytes)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                // TODO: This needs to be different for skole+ tier
                Errors = { ["file"] = ["Lagerkvoten er nået (100 GB). Slet filer for at frigøre plads."] }
            });
        }

        // Validate courseId if provided
        if (courseId.HasValue)
        {
            var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId.Value, ct);
            if (!courseExists)
            {
                return ValidationProblem(new ValidationProblemDetails
                {
                    Errors = { ["courseId"] = ["Faget findes ikke."] }
                });
            }
        }

        var uploaderName = http.HttpContext?.User.FindFirstValue("name")
            ?? http.HttpContext?.User.FindFirstValue("preferred_username")
            ?? "Ukendt";

        var fileId = Guid.NewGuid();
        var key = $"files/{tenant.TenantId}/{fileId}{ext}";
        var mimeType = file.ContentType is { Length: > 0 } ct2 ? ct2 : "application/octet-stream";

        await using var stream = file.OpenReadStream();
        var url = await storage.UploadAsync(key, mimeType, stream, ct);

        var schoolFile = new SchoolFile
        {
            Id = fileId,
            TenantId = tenant.TenantId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = mimeType,
            SizeBytes = file.Length,
            StorageKey = key,
            Url = url,
            CourseId = courseId,
            UploadedBy = uploaderName,
        };

        db.SchoolFiles.Add(schoolFile);
        await db.SaveChangesAsync(ct);

        string? courseName = null;
        if (courseId.HasValue)
        {
            courseName = await db.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        return CreatedAtAction(nameof(GetAll), new FileDto(
            schoolFile.Id,
            schoolFile.FileName,
            schoolFile.ContentType,
            schoolFile.SizeBytes,
            schoolFile.Url,
            schoolFile.CourseId,
            courseName,
            schoolFile.UploadedBy,
            schoolFile.UploadedAt));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var file = await db.SchoolFiles.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (file is null)
        {
            return NotFound();
        }

        db.SchoolFiles.Remove(file);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
