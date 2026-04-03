using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

/// <summary>Klasse — a group of students (e.g. 2.b, 9.a).</summary>
public sealed class Class : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
