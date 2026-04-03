using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

/// <summary>
/// One assigned lesson in a schema grid cell.
/// Weekday + TimeSlot → Course + Teacher + optional Room + optional Aide.
/// </summary>
public sealed class SchemaSlot : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid SchemaId { get; set; }
    public Schema Schema { get; set; } = null!;

    public Guid TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;

    /// <summary>Day of week: 1=Monday … 5=Friday.</summary>
    public int Weekday { get; set; }

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public Staff Teacher { get; set; } = null!;

    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid? AideId { get; set; }
    public Staff? Aide { get; set; }
}
