using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Skoleplanen.Api.Models;

public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled,
    Unpaid,
}

/// <summary>
/// Stripe subscription record for a school. Not tenant-scoped (no global query filter) —
/// accessed directly by SchoolId.
/// </summary>
[Index(nameof(StripeSubscriptionId))]
[Index(nameof(StripeCustomerId))]
public sealed class Subscription : IEntityTypeConfiguration<Subscription>
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(School))]
    public Guid SchoolId { get; set; }

    public SubscriptionStatus Status { get; set; }

    /// <summary>Stripe customer ID (cus_xxx)</summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>Stripe subscription ID (sub_xxx)</summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>When the current billing period ends (null during trial)</summary>
    public DateTimeOffset? CurrentPeriodEnd { get; set; }

    /// <summary>When the trial ends (14 days from school creation)</summary>
    public DateTimeOffset TrialEnd { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
    }
}
