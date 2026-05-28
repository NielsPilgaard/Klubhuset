using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public enum VacationRegistrationGranularity { Weeks, Days }

public sealed class VacationRegistrationWindow : ITenantScoped, IEntityTypeConfiguration<VacationRegistrationWindow>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Title { get; set; }

	public DateOnly RegistrationDeadline { get; set; }
	public DateOnly CareStartDate { get; set; }
	public DateOnly CareEndDate { get; set; }

	public VacationRegistrationGranularity Granularity { get; set; } = VacationRegistrationGranularity.Weeks;

	public bool IsOpen { get; set; }

	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	public ICollection<VacationRegistrationEntry> Entries { get; set; } = [];

	public void Configure(EntityTypeBuilder<VacationRegistrationWindow> builder)
	{
		builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");
	}
}

public sealed class VacationRegistrationEntry : ITenantScoped, IEntityTypeConfiguration<VacationRegistrationEntry>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid WindowId { get; set; }
	public VacationRegistrationWindow Window { get; set; } = null!;

	public Guid StudentId { get; set; }
	public Student Student { get; set; } = null!;

	public Guid SubmittedByParentId { get; set; }
	public Parent SubmittedByParent { get; set; } = null!;

	// Comma-separated ISO dates (yyyy-MM-dd).
	// Weeks mode: Monday of each selected week — e.g. "2026-06-29,2026-07-06"
	// Days mode: individual weekdays — e.g. "2026-04-06,2026-04-07,2026-04-10"
	[StringLength(4000)]
	public string SelectedDates { get; set; } = string.Empty;

	[StringLength(500)]
	public string? Note { get; set; }

	public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

	public void Configure(EntityTypeBuilder<VacationRegistrationEntry> builder)
	{
		builder.Property(e => e.SubmittedAt).HasDefaultValueSql("now()");
		builder.Property(e => e.UpdatedAt).ValueGeneratedNever();
		builder.HasOne(e => e.Window)
			.WithMany(w => w.Entries)
			.HasForeignKey(e => e.WindowId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasOne(e => e.Student)
			.WithMany()
			.HasForeignKey(e => e.StudentId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasOne(e => e.SubmittedByParent)
			.WithMany()
			.HasForeignKey(e => e.SubmittedByParentId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(e => new { e.TenantId, e.WindowId, e.StudentId }).IsUnique();
	}
}
