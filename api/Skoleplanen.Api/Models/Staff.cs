using System.ComponentModel.DataAnnotations;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

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

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(500)]
	public string? Email { get; set; }

	[StringLength(50)]
	public string? Phone { get; set; }

	public StaffRole Role { get; set; }

	/// <summary>Keycloak subject claim — null until the staff member accepts their invite.</summary>
	[StringLength(500)]
	public string? KeycloakSubject { get; set; }

	/// <summary>Whether this staff member holds the Keycloak 'admin' realm role.</summary>
	public bool IsAdmin { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
