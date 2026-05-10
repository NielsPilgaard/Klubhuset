using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

/// <summary>Join table: attaches an existing SchoolFile to a WeekPlanSlot.</summary>
public sealed class WeekPlanSlotFile : ITenantScoped, IEntityTypeConfiguration<WeekPlanSlotFile>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid WeekPlanSlotId { get; set; }
	public WeekPlanSlot WeekPlanSlot { get; set; } = null!;
	public Guid SchoolFileId { get; set; }
	public SchoolFile SchoolFile { get; set; } = null!;

	public void Configure(EntityTypeBuilder<WeekPlanSlotFile> builder)
	{
		builder.HasOne(f => f.WeekPlanSlot).WithMany(s => s.Files).HasForeignKey(f => f.WeekPlanSlotId).OnDelete(DeleteBehavior.Cascade);
		builder.HasOne(f => f.SchoolFile).WithMany().HasForeignKey(f => f.SchoolFileId).OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(f => new { f.WeekPlanSlotId, f.SchoolFileId }).IsUnique();
	}
}
