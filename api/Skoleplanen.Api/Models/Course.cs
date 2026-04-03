using Skoleplanen.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api.Models;

/// <summary>Fag — a subject (e.g. dansk, matematik, idræt).</summary>
public sealed class Course : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Name { get; set; }

	[StringLength(8000)]
	public string? Description { get; set; }

	public DateTimeOffset CreatedAt { get; init; }
}
