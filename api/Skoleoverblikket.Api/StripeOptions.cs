using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api;

public sealed class StripeOptions
{
	public const string SectionName = "Stripe";

	[Required(AllowEmptyStrings = false)]
	public string SecretKey { get; init; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string BasePriceIdMonthly { get; init; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string BasePriceIdYearly { get; init; } = string.Empty;

	[Required(AllowEmptyStrings = false)]
	public string WebhookSecret { get; init; } = string.Empty;

	/// <summary>Outer key: module name (e.g. "ParentModule"). Inner key: billing interval (e.g. "Monthly").</summary>
	public Dictionary<string, Dictionary<string, string>> ModulePriceIds { get; init; } = new();

	/// <summary>Override for Stripe's API base URL. Unset in production (uses Stripe's real API);
	/// set to a stripe-mock container's endpoint in local dev via Aspire, or in tests.</summary>
	public string? ApiBase { get; init; }
}
