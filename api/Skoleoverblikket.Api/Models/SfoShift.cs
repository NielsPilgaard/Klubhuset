using Skoleoverblikket.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Models;

/// <summary>SFO vagtblok — staff coverage block outside school hours.</summary>
public sealed class SfoShift : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	/// <summary>Day of week: 1=Monday … 5=Friday.</summary>
	[Range(1, 5)]
	public int DayOfWeek { get; set; }

	public TimeOnly StartTime { get; set; }
	public TimeOnly EndTime { get; set; }

	[StringLength(200)]
	public string? Label { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	public ICollection<SfoShiftStaff> StaffAssignments { get; set; } = [];
}

/// <summary>Many-to-many: which staff members cover a given SFO shift.</summary>
public sealed class SfoShiftStaff : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid ShiftId { get; set; }
	public SfoShift Shift { get; set; } = null!;

	public Guid StaffId { get; set; }
	public Staff Staff { get; set; } = null!;
}
