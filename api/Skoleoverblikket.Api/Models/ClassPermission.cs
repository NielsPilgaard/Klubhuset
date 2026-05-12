using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

/// <summary>
/// Grants a specific staff member write access to a specific class's schema and week plan.
/// When no ClassPermission rows exist for a tenant, all admins have full access (superadmin mode).
/// When rows exist, only admins explicitly listed can edit those classes.
/// </summary>
public sealed class ClassPermission : ITenantScoped, IEntityTypeConfiguration<ClassPermission>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid ClassId { get; set; }
	public Class Class { get; set; } = null!;

	public Guid StaffId { get; set; }
	public Staff Staff { get; set; } = null!;

	public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<ClassPermission> builder)
	{
		builder.HasIndex(p => new { p.TenantId, p.ClassId, p.StaffId }).IsUnique();

		builder.HasOne(p => p.Class)
			   .WithMany()
			   .HasForeignKey(p => p.ClassId)
			   .OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(p => p.Staff)
			   .WithMany()
			   .HasForeignKey(p => p.StaffId)
			   .OnDelete(DeleteBehavior.Cascade);
	}
}
