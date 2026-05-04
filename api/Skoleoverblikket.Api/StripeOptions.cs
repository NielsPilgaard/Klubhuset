using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string PriceId { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string WebhookSecret { get; init; } = string.Empty;
}
