using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

public sealed class Parent : ITenantScoped, IEntityTypeConfiguration<Parent>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(500)]
	public required string Email { get; set; }

	[StringLength(50)]
	public string? Phone { get; set; }

	[StringLength(500)]
	public string? Address { get; set; }

	[StringLength(10)]
	public string? PostalCode { get; set; }

	[StringLength(100)]
	public string? City { get; set; }

	/// <summary>Consent for future parent directory feature. Default off.</summary>
	public bool ShareContactInfo { get; set; }

	public bool AdresseBeskyttet { get; set; }

	[StringLength(2000)]
	public string? AvatarUrl { get; set; }

	[StringLength(128)]
	public string? KeycloakSubject { get; set; }

	public ICollection<Student> Students { get; set; } = [];

	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<Parent> builder)
	{
		builder.HasMany(p => p.Students)
			   .WithMany(s => s.Parents)
			   .UsingEntity<ParentStudent>(
				   "ParentStudents",
				   r => r.HasOne<Student>().WithMany().HasForeignKey(ps => ps.StudentId).OnDelete(DeleteBehavior.Cascade),
				   l => l.HasOne<Parent>().WithMany().HasForeignKey(ps => ps.ParentId).OnDelete(DeleteBehavior.Cascade),
				   j =>
				   {
					   j.HasKey(ps => new { ps.ParentId, ps.StudentId });
					   j.HasIndex(ps => new { ps.TenantId, ps.ParentId, ps.StudentId }).IsUnique();
				   });
	}
}

public sealed class ParentStudent
{
	public Guid TenantId { get; set; }
	public Guid ParentId { get; set; }
	public Guid StudentId { get; set; }
}
