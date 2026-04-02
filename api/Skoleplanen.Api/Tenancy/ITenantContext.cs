namespace Skoleplanen.Api.Tenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
}
