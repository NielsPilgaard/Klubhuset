using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

/// <summary>Fag — a subject (e.g. dansk, matematik, idræt).</summary>
public sealed class Course : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
}
