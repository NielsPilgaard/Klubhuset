using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

public enum AbsenceStatus { Reported, Confirmed, Dismissed }

public sealed class AbsenceReport : ITenantScoped, IEntityTypeConfiguration<AbsenceReport>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid StudentId { get; set; }
	public Student Student { get; set; } = null!;
	public Guid ReportedByParentId { get; set; }
	public Parent ReportedByParent { get; set; } = null!;
	public DateOnly Date { get; set; }
	public DateOnly? EndDate { get; set; }

	[StringLength(500)]
	public string? Reason { get; set; }

	public AbsenceStatus Status { get; set; } = AbsenceStatus.Reported;
	public Guid? ConfirmedByStaffId { get; set; }
	public Staff? ConfirmedByStaff { get; set; }
	public DateTimeOffset? ConfirmedAt { get; set; }
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<AbsenceReport> builder)
	{
		builder.HasOne(a => a.Student)
			.WithMany()
			.HasForeignKey(a => a.StudentId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(a => a.ReportedByParent)
			.WithMany()
			.HasForeignKey(a => a.ReportedByParentId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(a => a.ConfirmedByStaff)
			.WithMany()
			.HasForeignKey(a => a.ConfirmedByStaffId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(a => new { a.TenantId, a.Date });
		builder.HasIndex(a => new { a.TenantId, a.Status });
	}
}
