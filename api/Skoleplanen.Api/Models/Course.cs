using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleplanen.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api.Models;

/// <summary>Fag — a subject (e.g. dansk, matematik, idræt).</summary>
public sealed class Course : ITenantScoped, IEntityTypeConfiguration<Course>
{
	public Guid Id { get; init; }
	public Guid TenantId { get; init; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(8000)]
	public string? Description { get; set; }

	public DateTimeOffset CreatedAt { get; init; }

	public void Configure(EntityTypeBuilder<Course> builder) => builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
}
