using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

public enum SchemaStatus
{
    Draft,
    Complete
}

/// <summary>
/// Skema — the weekly schedule for a class.
/// A class can have multiple schemas (e.g. one per term), but only one active at a time.
/// </summary>
public sealed class Schema : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public required string Name { get; set; }
    public SchemaStatus Status { get; set; } = SchemaStatus.Draft;
    public bool IsActive { get; set; }

    public ICollection<SchemaSlot> Slots { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
