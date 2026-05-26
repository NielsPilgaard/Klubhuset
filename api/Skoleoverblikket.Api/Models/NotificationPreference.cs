using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Models;

public sealed class NotificationPreference : ITenantScoped, IEntityTypeConfiguration<NotificationPreference>
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid UserId { get; set; }
	public RecipientType UserType { get; set; }
	public NotificationType Type { get; set; }
	public bool InApp { get; set; } = true;
	public bool Email { get; set; } = true;

	public void Configure(EntityTypeBuilder<NotificationPreference> builder)
	{
		builder.HasIndex(p => new { p.TenantId, p.UserId, p.UserType, p.Type }).IsUnique();
	}
}
