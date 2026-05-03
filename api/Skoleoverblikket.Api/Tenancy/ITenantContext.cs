namespace Skoleoverblikket.Api.Tenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
}
