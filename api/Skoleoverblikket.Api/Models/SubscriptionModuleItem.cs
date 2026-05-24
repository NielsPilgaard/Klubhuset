using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Skoleoverblikket.Api.Models;

public sealed class SubscriptionModuleItem : IEntityTypeConfiguration<SubscriptionModuleItem>
{
	public Guid Id { get; set; }
	public Guid SubscriptionId { get; set; }
	public Subscription Subscription { get; set; } = null!;
	public SubscriptionModule Module { get; set; }

	/// <summary>Stripe subscription item ID (si_xxx). Null for admin overrides.</summary>
	public string? StripeSubscriptionItemId { get; set; }

	/// <summary>True when access is granted by admin override, not Stripe purchase.</summary>
	public bool IsAdminOverride { get; set; }

	public DateTimeOffset CreatedAt { get; init; }

	public void Configure(EntityTypeBuilder<SubscriptionModuleItem> builder)
	{
		builder.HasIndex(m => new { m.SubscriptionId, m.Module }).IsUnique();
		builder.Property(m => m.CreatedAt).HasDefaultValueSql("now()");
		builder.HasOne(m => m.Subscription)
			.WithMany(s => s.ActiveModules)
			.HasForeignKey(m => m.SubscriptionId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
