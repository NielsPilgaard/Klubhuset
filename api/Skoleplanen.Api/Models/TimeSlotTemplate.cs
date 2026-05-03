using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

/// <summary>
/// School-level default lesson structure.
/// Defines the default time slot grid inherited by all classes.
/// Per-class overrides are stored as TimeSlot records linked to the class.
/// </summary>
public sealed class TimeSlotTemplate : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Default lesson duration in minutes (e.g. 45).</summary>
    public int LessonDurationMinutes { get; set; }

    /// <summary>School day start time (e.g. 08:00).</summary>
    public TimeOnly DayStartTime { get; set; }

    /// <summary>School day end time (e.g. 15:30).</summary>
    public TimeOnly DayEndTime { get; set; }

    /// <summary>Which weekdays are active (1=Mon … 5=Fri). Stored as comma-separated ints.</summary>
    public string ActiveDays { get; set; } = "1,2,3,4,5";

    public ICollection<TimeSlotTemplateBreak> Breaks { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>A break slot within the school-level time slot template.</summary>
public sealed class TimeSlotTemplateBreak : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TimeSlotTemplateId { get; set; }
    public TimeSlotTemplate TimeSlotTemplate { get; set; } = null!;

    /// <summary>Break start time (e.g. 09:45).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Break duration in minutes.</summary>
    public int DurationMinutes { get; set; }
}
