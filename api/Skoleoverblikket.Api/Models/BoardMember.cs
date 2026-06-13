using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public sealed class BoardMember : ITenantScoped, IEntityTypeConfiguration<BoardMember>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200)]
	public required string Name { get; set; }

	[StringLength(500)]
	public required string Email { get; set; }

	[StringLength(500)]
	public string? KeycloakSubject { get; set; }

	public bool CanAccessTeacherData { get; set; }

	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<BoardMember> builder) =>
		builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
}
