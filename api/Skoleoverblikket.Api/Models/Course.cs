using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

/// <summary>Fag — a subject (e.g. dansk, matematik, idræt).</summary>
public sealed class Course : ITenantScoped, IEntityTypeConfiguration<Course>
{
	public Guid Id { get; init; }
	public Guid TenantId { get; init; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(8000)]
	public string? Description { get; set; }

	/// <summary>Hex color code, e.g. "#3b82f6". Null means use the auto-assigned palette color.</summary>
	[StringLength(7)]
	public string? Color { get; set; }

	public DateTimeOffset CreatedAt { get; init; }

	public void Configure(EntityTypeBuilder<Course> builder) => builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
}
