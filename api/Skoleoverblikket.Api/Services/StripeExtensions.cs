using Microsoft.Extensions.Options;
using Stripe;

namespace Skoleoverblikket.Api.Services;

public static class StripeExtensions
{
	public static IServiceCollection AddStripe(this IServiceCollection services)
	{
		services.AddOptions<StripeOptions>()
			.BindConfiguration(StripeOptions.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();
		services.AddSingleton<IValidateOptions<StripeOptions>, StripeOptionsValidator>();

		// Shared StripeClient — ApiBase can be overridden (local dev via Aspire, or tests) to
		// point at a stripe-mock container instead of the real Stripe API. Individual *Service
		// classes below resolve this client via DI rather than hitting Stripe's global
		// StripeConfiguration.ApiKey.
		services.AddSingleton<IStripeClient>(sp =>
		{
			var options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
			return string.IsNullOrEmpty(options.ApiBase)
				? new StripeClient(options.SecretKey)
				: new StripeClient(apiKey: options.SecretKey, apiBase: options.ApiBase);
		});

		services.AddSingleton<CustomerService>();
		services.AddSingleton<Stripe.Checkout.SessionService>();
		services.AddSingleton<Stripe.BillingPortal.SessionService>();
		services.AddSingleton<SubscriptionItemService>();
		services.AddSingleton<Stripe.SubscriptionService>();

		return services;
	}
}
