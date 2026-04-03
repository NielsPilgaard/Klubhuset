using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

/// <summary>Lokale — a physical room (e.g. "Lokale 12", "Gymnastiksalen").</summary>
public sealed class Room : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public int? Capacity { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
