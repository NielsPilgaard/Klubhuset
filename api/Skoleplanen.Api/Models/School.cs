using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

/// <summary>
/// Represents a tenant (school). TenantId == Id for the root entity.
/// </summary>
public sealed class School : ITenantScoped, IEntityTypeConfiguration<School>
{
	public Guid Id { get; set; }
	public Guid TenantId => Id;

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(500)]
	public string? ContactEmail { get; set; }

	[StringLength(50)]
	public string? ContactPhone { get; set; }

	[StringLength(500)]
	public string? LogoUrl { get; set; }

	public DateTimeOffset CreatedAt { get; init; }

	public void Configure(EntityTypeBuilder<School> builder) => builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
}
