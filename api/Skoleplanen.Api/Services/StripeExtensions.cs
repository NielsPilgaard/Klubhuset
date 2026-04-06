using Stripe;

namespace Skoleplanen.Api.Services;

public static class StripeExtensions
{
    public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Stripe services
        services.AddSingleton<CustomerService>();
        services.AddSingleton<Stripe.Checkout.SessionService>();
        services.AddSingleton<Stripe.BillingPortal.SessionService>();

        // Configure Stripe global API key from strongly-typed options
        var stripeOptions = configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
        if (!string.IsNullOrEmpty(stripeOptions?.SecretKey))
        {
            StripeConfiguration.ApiKey = stripeOptions.SecretKey;
        }

        return services;
    }
}
