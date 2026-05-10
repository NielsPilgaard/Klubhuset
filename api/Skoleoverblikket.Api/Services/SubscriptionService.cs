using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Stripe;
using Stripe.Checkout;
using LocalSubscription = Skoleoverblikket.Api.Models.Subscription;
using StripeSubscription = Stripe.Subscription;

namespace Skoleoverblikket.Api.Services;

public sealed class SubscriptionService(
	AppDbContext db,
	IOptions<StripeOptions> stripeOptions,
	ILogger<SubscriptionService> logger,
	CustomerService customerService,
	SessionService sessionService,
	Stripe.BillingPortal.SessionService billingPortalSessionService)
{
	private const int TrialDays = 14;

	/// <summary>
	/// Returns the subscription for the given school, creating a Trialing record if none exists.
	/// </summary>
	public async Task<LocalSubscription> GetOrCreateAsync(Guid schoolId, CancellationToken ct = default)
	{
		var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId, ct);
		if (sub is not null)
		{
			return sub;
		}

		var school = await db.Schools.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == schoolId, ct)
					 ?? throw new InvalidOperationException($"School {schoolId} not found");

		sub = new LocalSubscription
		{
			Id = Guid.NewGuid(),
			SchoolId = schoolId,
			Status = SubscriptionStatus.Trialing,
			TrialEnd = DateTimeOffset.UtcNow.AddDays(TrialDays),
		};

		db.Subscriptions.Add(sub);

		try
		{
			await db.SaveChangesAsync(ct);
			return sub;
		}
		catch (DbUpdateException)
		{
			// Race condition: another request created the subscription. Fetch and return it.
			var existingSub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId, ct);
			if (existingSub is not null)
			{
				return existingSub;
			}

			throw;
		}
	}

	/// <summary>
	/// Creates a Stripe Checkout session for the given school.
	/// </summary>
	public async Task<string> CreateCheckoutSessionAsync(
		Guid schoolId,
		string successUrl,
		string cancelUrl,
		CancellationToken ct = default)
	{
		var sub = await GetOrCreateAsync(schoolId, ct);
		var school = await db.Schools.IgnoreQueryFilters().FirstAsync(s => s.Id == schoolId, ct);

		var priceId = stripeOptions.Value.PriceId;

		// Ensure Stripe customer exists
		var customerId = sub.StripeCustomerId;
		if (customerId is null)
		{
			var customer = await customerService.CreateAsync(new CustomerCreateOptions
			{
				Email = school.ContactEmail,
				Name = school.Name,
				Metadata = new Dictionary<string, string> { ["school_id"] = schoolId.ToString() },
			}, cancellationToken: ct);
			customerId = customer.Id;
			sub.StripeCustomerId = customerId;
			await db.SaveChangesAsync(ct);
		}

		var options = new SessionCreateOptions
		{
			Customer = customerId,
			Mode = "subscription",
			LineItems =
			[
				new SessionLineItemOptions
				{
					Price = priceId,
					Quantity = 1,
				},
			],
			SuccessUrl = successUrl,
			CancelUrl = cancelUrl,
			Metadata = new Dictionary<string, string> { ["school_id"] = schoolId.ToString() },
			PaymentMethodCollection = "always",
			SubscriptionData = sub.Status == SubscriptionStatus.Trialing && sub.TrialEnd > DateTimeOffset.UtcNow
				? new SessionSubscriptionDataOptions
				{
					TrialEnd = sub.TrialEnd.UtcDateTime,
					Metadata = new Dictionary<string, string> { ["school_id"] = schoolId.ToString() },
				}
				: null,
		};

		var session = await sessionService.CreateAsync(options, cancellationToken: ct);

		return session.Url ?? throw new InvalidOperationException("Stripe session URL is null");
	}

	/// <summary>
	/// Creates a Stripe billing portal session for the given school.
	/// </summary>
	public async Task<string> CreateBillingPortalSessionAsync(
		Guid schoolId,
		string returnUrl,
		CancellationToken ct = default)
	{
		var sub = await GetOrCreateAsync(schoolId, ct);

		if (sub.StripeCustomerId is null)
		{
			throw new InvalidOperationException("No Stripe customer for this school — subscribe first.");
		}

		var session = await billingPortalSessionService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
		{
			Customer = sub.StripeCustomerId,
			ReturnUrl = returnUrl,
		}, cancellationToken: ct);

		return session.Url ?? throw new InvalidOperationException("Stripe billing portal session URL is null");
	}

	/// <summary>
	/// Processes a Stripe webhook event and updates the local subscription record.
	/// </summary>
	public async Task HandleWebhookAsync(Event stripeEvent, CancellationToken ct = default)
	{
		switch (stripeEvent.Type)
		{
			case EventTypes.CheckoutSessionCompleted:
				{
					if (stripeEvent.Data.Object is Session session)
					{
						await HandleCheckoutCompletedAsync(session, ct);
					}

					break;
				}

			case EventTypes.CustomerSubscriptionUpdated:
			case EventTypes.CustomerSubscriptionDeleted:
				{
					if (stripeEvent.Data.Object is StripeSubscription stripeSub)
					{
						await HandleSubscriptionChangedAsync(stripeSub, ct);
					}

					break;
				}

			case EventTypes.InvoicePaymentSucceeded:
				{
					if (stripeEvent.Data.Object is Invoice invoice)
					{
						await HandleInvoicePaymentSucceededAsync(invoice, ct);
					}

					break;
				}

			case EventTypes.InvoicePaymentFailed:
				{
					if (stripeEvent.Data.Object is Invoice failedInvoice)
					{
						await HandleInvoicePaymentFailedAsync(failedInvoice, ct);
					}

					break;
				}

			default:
				logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
				break;
		}
	}

	private async Task HandleCheckoutCompletedAsync(Session session, CancellationToken ct)
	{
		if (!session.Metadata.TryGetValue("school_id", out var schoolIdStr)
			|| !Guid.TryParse(schoolIdStr, out var schoolId))
		{
			logger.LogWarning("CheckoutSessionCompleted missing school_id metadata");
			return;
		}

		var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId, ct);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleCheckoutCompletedAsync: Subscription not found for SchoolId {SchoolId}, SessionId {SessionId}", schoolId, session.Id);
			return;
		}

		sub.StripeCustomerId ??= session.CustomerId;
		sub.StripeSubscriptionId = session.SubscriptionId;
		sub.Status = SubscriptionStatus.Active;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(ct);
	}

	private async Task HandleSubscriptionChangedAsync(StripeSubscription stripeSub, CancellationToken ct)
	{
		var sub = await db.Subscriptions.FirstOrDefaultAsync(
			s => s.StripeSubscriptionId == stripeSub.Id, ct);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleSubscriptionChangedAsync: Subscription not found for StripeSubscriptionId {StripeSubscriptionId}", stripeSub.Id);
			return;
		}

		sub.Status = stripeSub.Status switch
		{
			"active" => SubscriptionStatus.Active,
			"trialing" => SubscriptionStatus.Trialing,
			"past_due" => SubscriptionStatus.PastDue,
			"canceled" => SubscriptionStatus.Canceled,
			"unpaid" => SubscriptionStatus.Unpaid,
			_ => sub.Status,
		};

		// Stripe SDK returns DateTime.MinValue when unset — treat that as null
		sub.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd > DateTime.UnixEpoch
			? stripeSub.CurrentPeriodEnd
			: null;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(ct);
	}

	private async Task HandleInvoicePaymentSucceededAsync(Invoice invoice, CancellationToken ct)
	{
		if (invoice.SubscriptionId is null)
		{
			return;
		}

		var sub = await db.Subscriptions.FirstOrDefaultAsync(
			s => s.StripeSubscriptionId == invoice.SubscriptionId, ct);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleInvoicePaymentSucceededAsync: Subscription not found for StripeSubscriptionId {StripeSubscriptionId}, InvoiceId {InvoiceId}", invoice.SubscriptionId, invoice.Id);
			return;
		}

		sub.Status = SubscriptionStatus.Active;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(ct);
	}

	private async Task HandleInvoicePaymentFailedAsync(Invoice invoice, CancellationToken ct)
	{
		if (invoice.SubscriptionId is null)
		{
			return;
		}

		var sub = await db.Subscriptions.FirstOrDefaultAsync(
			s => s.StripeSubscriptionId == invoice.SubscriptionId, ct);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleInvoicePaymentFailedAsync: Subscription not found for StripeSubscriptionId {StripeSubscriptionId}, InvoiceId {InvoiceId}", invoice.SubscriptionId, invoice.Id);
			return;
		}

		sub.Status = SubscriptionStatus.PastDue;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(ct);
	}
}
