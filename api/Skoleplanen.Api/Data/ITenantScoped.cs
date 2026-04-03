namespace Skoleplanen.Api.Data;

/// <summary>
/// Marker interface for all entities that belong to a tenant.
/// AppDbContext automatically applies a global query filter for TenantId
/// on every entity that implements this interface.
/// </summary>
public interface ITenantScoped
{
	Guid TenantId { get; }
}
