using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleplanen.Api.Services;
using Stripe;
using LocalSubscriptionService = Skoleplanen.Api.Services.SubscriptionService;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/stripe/webhook")]
public sealed class StripeWebhookController(
    LocalSubscriptionService subscriptionService,
    IConfiguration config,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var webhookSecret = config["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            logger.LogError("Stripe:WebhookSecret not configured");
            return StatusCode(500);
        }

        string json;
        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);
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
