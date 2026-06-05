using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

/// <summary>Klasse — a group of students (e.g. 2.b, 9.a).</summary>
public sealed class Class : ITenantScoped, IArchivable
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(8000)]
	public string? Description { get; set; }

	/// <summary>Klassetrin: 0 = børnehaveklasse, 1–10 = 1.–10. klasse. Null means unknown.</summary>
	[Range(0, 10)]
	public int? GradeLevel { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? ArchivedAt { get; set; }
}
