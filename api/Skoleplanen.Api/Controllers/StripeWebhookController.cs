using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Services;
using Stripe;
using LocalSubscriptionService = Skoleoverblikket.Api.Services.SubscriptionService;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/stripe/webhook")]
public sealed class StripeWebhookController(
    LocalSubscriptionService subscriptionService,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        string json;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Stripe webhook received without Stripe-Signature header");
            return BadRequest();
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                stripeOptions.Value.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning("Stripe webhook signature validation failed: {Message}", ex.Message);
            return BadRequest();
        }

        await subscriptionService.HandleWebhookAsync(stripeEvent, ct);
        return Ok();
    }
}
