using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

/// <summary>
/// One assigned lesson in a schema grid cell.
/// Weekday + TimeSlot → Course + Teacher + optional Room + optional Aide.
/// </summary>
public sealed class SchemaSlot : ITenantScoped, IEntityTypeConfiguration<SchemaSlot>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid SchemaId { get; set; }
	public Schema Schema { get; set; } = null!;

	public Guid TimeSlotId { get; set; }
	public TimeSlot TimeSlot { get; set; } = null!;

	/// <summary>Day of week: 0=Sunday, 1=Monday … 5=Friday.</summary>
	public DayOfWeek Weekday { get; set; }

	public Guid CourseId { get; set; }
	public Course Course { get; set; } = null!;

	public Guid TeacherId { get; set; }
	public Staff Teacher { get; set; } = null!;

	public Guid? RoomId { get; set; }
	public Room? Room { get; set; }

	public Guid? AideId { get; set; }
	public Staff? Aide { get; set; }

	// SchemaSlot has two FKs to Staff (Teacher and Aide) — configure explicitly
	public void Configure(EntityTypeBuilder<SchemaSlot> builder)
	{
		builder.HasOne(s => s.Teacher)
		       .WithMany()
		       .HasForeignKey(s => s.TeacherId)
		       .OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(s => s.Aide)
		       .WithMany()
		       .HasForeignKey(s => s.AideId)
		       .OnDelete(DeleteBehavior.Restrict);
	}
}
