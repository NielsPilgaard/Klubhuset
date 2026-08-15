using Stripe;

namespace Skoleoverblikket.Api.Services;

public static class StripeExtensions
{
	public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<StripeOptions>()
			.BindConfiguration(StripeOptions.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

		// Shared StripeClient — ApiBase can be overridden (local dev via Aspire, or tests) to
		// point at a stripe-mock container instead of the real Stripe API. Individual *Service
		// classes below resolve this client via DI rather than hitting Stripe's global
		// StripeConfiguration.ApiKey.
		services.AddSingleton(sp =>
		{
			var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StripeOptions>>().Value;
			return string.IsNullOrEmpty(options.ApiBase)
				? new StripeClient(options.SecretKey)
				: new StripeClient(apiKey: options.SecretKey, apiBase: options.ApiBase);
		});

		services.AddSingleton(sp => new CustomerService(sp.GetRequiredService<StripeClient>()));
		services.AddSingleton(sp => new Stripe.Checkout.SessionService(sp.GetRequiredService<StripeClient>()));
		services.AddSingleton(sp => new Stripe.BillingPortal.SessionService(sp.GetRequiredService<StripeClient>()));
		services.AddSingleton(sp => new SubscriptionItemService(sp.GetRequiredService<StripeClient>()));

		return services;
	}
}
