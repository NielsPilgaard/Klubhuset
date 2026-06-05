using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Models;

public sealed class Notification : ITenantScoped, IEntityTypeConfiguration<Notification>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid RecipientId { get; set; }
	public RecipientType RecipientType { get; set; }
	public NotificationType Type { get; set; }
	public Guid? ReferenceId { get; set; }

	[MaxLength(300)]
	public required string Body { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? ReadAt { get; set; }

	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder.HasIndex(n => new { n.TenantId, n.RecipientId, n.ReadAt });
		builder.HasIndex(n => new { n.TenantId, n.RecipientId, n.CreatedAt });
	}
}
