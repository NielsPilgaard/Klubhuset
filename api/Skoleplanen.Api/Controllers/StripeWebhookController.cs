using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Skoleplanen.Api.Services;
using Stripe;
using LocalSubscriptionService = Skoleplanen.Api.Services.SubscriptionService;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/stripe/webhook")]
public sealed class StripeWebhookController(
    LocalSubscriptionService subscriptionService,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
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
                stripeOptions.Value.WebhookSecret);
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
