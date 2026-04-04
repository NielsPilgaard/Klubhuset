using System.ComponentModel.DataAnnotations;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

/// <summary>Fil — a file uploaded by staff or admin, optionally linked to a course.</summary>
public sealed class SchoolFile : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [StringLength(500, MinimumLength = 1)]
    public required string FileName { get; set; }

    [StringLength(200)]
    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Key in object storage.</summary>
    [StringLength(1000)]
    public required string StorageKey { get; set; }

    /// <summary>Public URL returned by object storage.</summary>
    [StringLength(2000)]
    public required string Url { get; set; }

    /// <summary>Optional link to a course (fag).</summary>
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }

    [StringLength(200)]
    public required string UploadedBy { get; set; }

    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
}
