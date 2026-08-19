using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api;

public sealed class StripeOptionsValidator : IValidateOptions<StripeOptions>
{
	public ValidateOptionsResult Validate(string? name, StripeOptions options)
	{
		var errors = new List<string>();

		foreach (var module in Enum.GetValues<SubscriptionModule>())
		{
			foreach (var interval in Enum.GetValues<BillingInterval>())
			{
				var moduleName = module.ToString();
				var intervalName = interval.ToString();
				if (!options.ModulePriceIds.TryGetValue(moduleName, out var intervalPrices)
					|| !intervalPrices.TryGetValue(intervalName, out var priceId)
					|| string.IsNullOrEmpty(priceId))
				{
					errors.Add($"Stripe:ModulePriceIds is missing a price for {moduleName}:{intervalName}.");
				}
			}
		}

		return errors.Count > 0
			? ValidateOptionsResult.Fail(errors)
			: ValidateOptionsResult.Success;
	}
}
