using System.ComponentModel.DataAnnotations;
using Skoleoverblikket.Api.Data;

namespace Skoleoverblikket.Api.Models;

public sealed class BroadcastEmail : ITenantScoped
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	public Guid SenderStaffId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string SenderName { get; set; }

	public Guid? ClassId { get; set; }

	[StringLength(200, MinimumLength = 1)]
	public required string Subject { get; set; }

	[StringLength(10000, MinimumLength = 1)]
	public required string Body { get; set; }

	public int RecipientCount { get; set; }

	public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}
