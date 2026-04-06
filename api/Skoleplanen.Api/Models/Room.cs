using System.ComponentModel.DataAnnotations;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Models;

/// <summary>Lokale — a physical room (e.g. "Lokale 12", "Gymnastiksalen").</summary>
public sealed class Room : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(8000)]
	public string? Description { get; set; }

	public int? Capacity { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
