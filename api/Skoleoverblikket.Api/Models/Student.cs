using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

public sealed class Student : ITenantScoped, IEntityTypeConfiguration<Student>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	public Guid ClassId { get; set; }
	public Class Class { get; set; } = null!;

	public ICollection<Parent> Parents { get; set; } = [];

	[StringLength(2000)]
	public string? AvatarUrl { get; set; }

	public bool IsEnrolledInSfo { get; set; }

	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<Student> builder)
	{
		builder.HasOne(s => s.Class)
			   .WithMany()
			   .HasForeignKey(s => s.ClassId)
			   .OnDelete(DeleteBehavior.Restrict);
	}
}
