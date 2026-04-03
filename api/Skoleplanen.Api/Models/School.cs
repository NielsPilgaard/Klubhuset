using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

/// <summary>
/// Represents a tenant (school). TenantId == Id for the root entity.
/// </summary>
public sealed class School : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId => Id;

    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? LogoUrl { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
}
