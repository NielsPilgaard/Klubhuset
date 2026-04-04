using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Services;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize(Roles = "admin")]
public sealed class BillingController(
    SubscriptionService subscriptionService,
    ITenantContext tenantContext) : ControllerBase
{
    public record SubscriptionDto(
        SubscriptionStatus Status,
        DateTimeOffset TrialEnd,
        DateTimeOffset? CurrentPeriodEnd,
        bool IsTrialing,
        bool IsActive,
        bool HasAccess,
        int TrialDaysLeft);

    public record CheckoutRequest(string SuccessUrl, string CancelUrl);
    public record PortalRequest(string ReturnUrl);
    public record CheckoutResponse(string Url);

    [HttpGet("subscription")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(CancellationToken ct)
    {
        var sub = await subscriptionService.GetOrCreateAsync(tenantContext.TenantId, ct);
        return Ok(ToDto(sub));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> CreateCheckout(
        [FromBody] CheckoutRequest req,
        CancellationToken ct)
    {
        var url = await subscriptionService.CreateCheckoutSessionAsync(
            tenantContext.TenantId, req.SuccessUrl, req.CancelUrl, ct);
        return Ok(new CheckoutResponse(url));
    }

    [HttpPost("portal")]
    public async Task<ActionResult<CheckoutResponse>> CreatePortal(
        [FromBody] PortalRequest req,
        CancellationToken ct)
    {
        var url = await subscriptionService.CreateBillingPortalSessionAsync(
            tenantContext.TenantId, req.ReturnUrl, ct);
        return Ok(new CheckoutResponse(url));
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
