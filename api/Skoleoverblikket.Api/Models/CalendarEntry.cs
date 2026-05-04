using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public enum CalendarEntryType { Ferie, Lukkedag, Arbejdsdag, Begivenhed }

public sealed class CalendarEntry : ITenantScoped, IEntityTypeConfiguration<CalendarEntry>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public CalendarEntryType Type { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Title { get; set; }

	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	// RRULE string, e.g. "FREQ=WEEKLY;INTERVAL=2" or "FREQ=MONTHLY"
	public string? RecurrenceRule { get; set; }

	// Inclusive end date for recurrence expansion (null = no recurrence)
	public DateOnly? RecurrenceEnd { get; set; }

	public void Configure(EntityTypeBuilder<CalendarEntry> builder)
	{
		builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
		builder.Property(e => e.Type).HasConversion<int>();
		builder.HasIndex(e => new { e.TenantId, e.StartDate, e.EndDate });
	}
}
