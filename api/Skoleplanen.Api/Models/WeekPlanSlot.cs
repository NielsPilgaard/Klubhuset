using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

public sealed class WeekPlanSlot : ITenantScoped, IEntityTypeConfiguration<WeekPlanSlot>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WeekPlanId { get; set; }
    public WeekPlan WeekPlan { get; set; } = null!;
    public Guid SchemaSlotId { get; set; }
    public SchemaSlot SchemaSlot { get; set; } = null!;

    [StringLength(8000)]
    public string? Beskrivelse { get; set; }

    [StringLength(8000)]
    public string? Lektier { get; set; }

    /// <summary>Course override for this week. Null = use SchemaSlot.Course.</summary>
    public Guid? FagSwapCourseId { get; set; }
    public Course? FagSwapCourse { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<WeekPlanSlotFile> Files { get; set; } = [];

    public void Configure(EntityTypeBuilder<WeekPlanSlot> builder)
    {
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
        builder.HasOne(s => s.WeekPlan).WithMany(w => w.Slots).HasForeignKey(s => s.WeekPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.SchemaSlot).WithMany().HasForeignKey(s => s.SchemaSlotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.FagSwapCourse).WithMany().HasForeignKey(s => s.FagSwapCourseId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(s => new { s.WeekPlanId, s.SchemaSlotId }).IsUnique();
    }
}
