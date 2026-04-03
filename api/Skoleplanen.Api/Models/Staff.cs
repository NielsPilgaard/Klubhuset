using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Domain;

public enum StaffRole
{
    Teacher,  // Lærer
    Aide,     // Pædagog
    Substitute // Vikar
}

public sealed class Staff : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public StaffRole Role { get; set; }

    /// <summary>Keycloak subject claim — null until the staff member accepts their invite.</summary>
    public string? KeycloakSubject { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
