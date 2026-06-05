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

		// Register Stripe services
		services.AddSingleton<CustomerService>();
		services.AddSingleton<Stripe.Checkout.SessionService>();
		services.AddSingleton<Stripe.BillingPortal.SessionService>();
		services.AddSingleton<SubscriptionItemService>();

		// Configure Stripe global API key from strongly-typed options
		var stripeOptions = configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
		if (!string.IsNullOrEmpty(stripeOptions?.SecretKey))
		{
			StripeConfiguration.ApiKey = stripeOptions.SecretKey;
		}

		return services;
	}
}
