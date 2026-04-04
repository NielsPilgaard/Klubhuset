using System.ComponentModel.DataAnnotations;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

/// <summary>
/// Lektion — a concrete time period in a class's weekly schema.
/// Inherited from the school's TimeSlotTemplate but can be overridden per class.
/// </summary>
public sealed class TimeSlot : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	/// <summary>The class this time slot belongs to. Null = school-level default template slot.</summary>
	public Guid? ClassId { get; set; }
	public Class? Class { get; set; }

	/// <summary>Order index within the day (1-based).</summary>
	public int SortOrder { get; set; }

	/// <summary>Start time of this lesson (e.g. 08:00).</summary>
	public TimeOnly StartTime { get; set; }

	/// <summary>End time of this lesson (e.g. 08:45).</summary>
	public TimeOnly EndTime { get; set; }

	/// <summary>Label shown on the grid (e.g. "1.", "2." or custom name).</summary>
	[StringLength(500)]
	public string? Label { get; set; }

	/// <summary>True when this row represents a break (non-assignable).</summary>
	public bool IsBreak { get; set; }
}
