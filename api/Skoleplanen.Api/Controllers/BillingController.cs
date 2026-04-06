using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Services;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize(Roles = "admin")]
public sealed class BillingController(
    SubscriptionService subscriptionService,
    ITenantContext tenantContext,
    IOptions<ApplicationOptions> appOptions) : ControllerBase
{
    public record SubscriptionDto(
        SubscriptionStatus Status,
        DateTimeOffset TrialEnd,
        DateTimeOffset? CurrentPeriodEnd,
        bool IsTrialing,
        bool IsActive,
        bool HasAccess,
        int TrialDaysLeft);

    public record CheckoutResponse(string Url);

    [HttpGet("subscription")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(CancellationToken ct)
    {
        var sub = await subscriptionService.GetOrCreateAsync(tenantContext.TenantId, ct);
        return Ok(ToDto(sub));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> CreateCheckout(CancellationToken ct)
    {
        var baseUrl = appOptions.Value.BaseUrl;
        var successUrl = $"{baseUrl}/abonnement?success=true";
        var cancelUrl = $"{baseUrl}/abonnement";

        try
        {
            var url = await subscriptionService.CreateCheckoutSessionAsync(
                tenantContext.TenantId, successUrl, cancelUrl, ct);
            return Ok(new CheckoutResponse(url));
        }
        catch (Stripe.StripeException ex)
        {
            return Problem(
                title: "Betalingsgateway fejl",
                detail: "Kunne ikke oprette betalingssession. Prøv igen eller kontakt support.",
                statusCode: StatusCodes.Status502BadGateway,
                extensions: new Dictionary<string, object?> { ["stripeCode"] = ex.StripeError?.Code });
        }
    }

    [HttpPost("portal")]
    public async Task<ActionResult<CheckoutResponse>> CreatePortal(CancellationToken ct)
    {
        var returnUrl = $"{appOptions.Value.BaseUrl}/abonnement";

        try
        {
            var url = await subscriptionService.CreateBillingPortalSessionAsync(
                tenantContext.TenantId, returnUrl, ct);
            return Ok(new CheckoutResponse(url));
        }
        catch (Stripe.StripeException ex)
        {
            return Problem(
                title: "Betalingsgateway fejl",
                detail: "Kunne ikke åbne betalingsportal. Prøv igen eller kontakt support.",
                statusCode: StatusCodes.Status502BadGateway,
                extensions: new Dictionary<string, object?> { ["stripeCode"] = ex.StripeError?.Code });
        }
    }

    private static SubscriptionDto ToDto(Subscription sub)
    {
        var now = DateTimeOffset.UtcNow;
        var isTrialing = sub.Status == SubscriptionStatus.Trialing && sub.TrialEnd > now;
        var isActive = sub.Status == SubscriptionStatus.Active;
        var hasAccess = isActive || isTrialing;
        var trialDaysLeft = isTrialing ? Math.Max(0, (int)Math.Ceiling((sub.TrialEnd - now).TotalDays)) : 0;

        return new SubscriptionDto(
            sub.Status,
            sub.TrialEnd,
            sub.CurrentPeriodEnd,
            isTrialing,
            isActive,
            hasAccess,
            trialDaysLeft);
    }
}
