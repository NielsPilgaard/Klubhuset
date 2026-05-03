using System.ComponentModel.DataAnnotations;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

/// <summary>
/// Skema — the weekly schedule for a class.
/// A class can have multiple schemas (e.g. one per term).
/// A schema is considered active when today falls within [StartDate, EndDate].
/// </summary>
public sealed class Schema : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid ClassId { get; set; }
	public Class Class { get; set; } = null!;

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }
	public DateOnly? StartDate { get; set; }
	public DateOnly? EndDate { get; set; }

	public ICollection<SchemaSlot> Slots { get; set; } = [];
	public ICollection<TimeSlot> TimeSlots { get; set; } = [];
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
