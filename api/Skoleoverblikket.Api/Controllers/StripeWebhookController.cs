using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Services;
using Stripe;
using LocalSubscriptionService = Skoleoverblikket.Api.Services.SubscriptionService;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/stripe/webhook")]
public sealed partial class StripeWebhookController(
	LocalSubscriptionService subscriptionService,
	IOptions<StripeOptions> stripeOptions,
	ILogger<StripeWebhookController> logger) : ControllerBase
{
	[HttpPost]
	[AllowAnonymous]
	[DisableRequestSizeLimit]
	public async Task<IActionResult> Handle(CancellationToken ct)
	{
		LogWebhookReceived(logger, HttpContext.Connection.RemoteIpAddress);

		string json;
		using (var reader = new StreamReader(Request.Body, leaveOpen: true))
		{
			json = await reader.ReadToEndAsync(ct);
		}

		LogPayloadSize(logger, json.Length);

		var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
		if (string.IsNullOrEmpty(signature))
		{
			LogMissingSignatureHeader(logger, HttpContext.Connection.RemoteIpAddress);
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
			LogSignatureValidationFailed(logger, HttpContext.Connection.RemoteIpAddress, ex.Message);
			return BadRequest();
		}

		LogWebhookAccepted(logger, stripeEvent.Type, stripeEvent.Id, stripeEvent.Created);

		try
		{
			await subscriptionService.HandleWebhookAsync(stripeEvent, ct);
			LogWebhookHandled(logger, stripeEvent.Type, stripeEvent.Id);
		}
		catch (Exception ex)
		{
			LogWebhookFailed(logger, ex, stripeEvent.Type, stripeEvent.Id);
			throw;
		}

		return Ok();
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Stripe webhook POST received from {RemoteIp}")]
	static partial void LogWebhookReceived(ILogger logger, System.Net.IPAddress? remoteIp);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Stripe webhook payload size: {Bytes} bytes")]
	static partial void LogPayloadSize(ILogger logger, int bytes);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Stripe webhook rejected: missing Stripe-Signature header from {RemoteIp}")]
	static partial void LogMissingSignatureHeader(ILogger logger, System.Net.IPAddress? remoteIp);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Stripe webhook rejected: signature validation failed from {RemoteIp}. {Message}")]
	static partial void LogSignatureValidationFailed(ILogger logger, System.Net.IPAddress? remoteIp, string message);

	[LoggerMessage(Level = LogLevel.Information, Message = "Stripe webhook accepted: EventType={EventType} EventId={EventId} Created={Created}")]
	static partial void LogWebhookAccepted(ILogger logger, string eventType, string eventId, DateTime created);

	[LoggerMessage(Level = LogLevel.Information, Message = "Stripe webhook handled: EventType={EventType} EventId={EventId}")]
	static partial void LogWebhookHandled(ILogger logger, string eventType, string eventId);

	[LoggerMessage(Level = LogLevel.Error, Message = "Stripe webhook processing failed: EventType={EventType} EventId={EventId}")]
	static partial void LogWebhookFailed(ILogger logger, Exception ex, string eventType, string eventId);
}
