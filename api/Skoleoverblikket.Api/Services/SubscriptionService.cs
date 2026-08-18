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
	Stripe.BillingPortal.SessionService billingPortalSessionService,
	SubscriptionItemService subscriptionItemService)
{
	private const int TrialDays = 14;

	/// <summary>
	/// Returns the subscription for the given school, creating a Trialing record if none exists.
	/// </summary>
	public async Task<LocalSubscription> GetOrCreateAsync(Guid schoolId, CancellationToken cancellationToken = default)
	{
		var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken);
		if (sub is not null)
		{
			return sub;
		}

		var school = await db.Schools.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken)
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
			await db.SaveChangesAsync(cancellationToken);
			return sub;
		}
		catch (DbUpdateException)
		{
			// Race condition: another request created the subscription. Fetch and return it.
			var existingSub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken);
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
		BillingInterval interval,
		string successUrl,
		string cancelUrl,
		CancellationToken cancellationToken = default)
	{
		var sub = await GetOrCreateAsync(schoolId, cancellationToken);
		var school = await db.Schools.IgnoreQueryFilters().FirstAsync(s => s.Id == schoolId, cancellationToken);

		var priceId = interval switch
		{
			BillingInterval.Monthly => stripeOptions.Value.BasePriceIdMonthly,
			BillingInterval.Yearly => stripeOptions.Value.BasePriceIdYearly,
			_ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported billing interval."),
		};

		// Interval is persisted only once checkout completes (HandleCheckoutCompletedAsync) —
		// writing it here would leave a stale Interval on the subscription if the customer
		// abandons checkout. The chosen interval travels via session metadata instead.

		// Ensure Stripe customer exists
		var customerId = sub.StripeCustomerId;
		if (customerId is null)
		{
			var customer = await customerService.CreateAsync(new CustomerCreateOptions
			{
				Email = school.ContactEmail,
				Name = school.Name,
				Metadata = new Dictionary<string, string> { ["school_id"] = schoolId.ToString() },
			}, cancellationToken: cancellationToken);
			customerId = customer.Id;
			sub.StripeCustomerId = customerId;
			await db.SaveChangesAsync(cancellationToken);
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
			Metadata = new Dictionary<string, string>
			{
				["school_id"] = schoolId.ToString(),
				["interval"] = interval.ToString(),
			},
			PaymentMethodCollection = "always",
			SubscriptionData = sub.Status == SubscriptionStatus.Trialing && sub.TrialEnd > DateTimeOffset.UtcNow
				? new SessionSubscriptionDataOptions
				{
					TrialEnd = sub.TrialEnd.UtcDateTime,
					Metadata = new Dictionary<string, string> { ["school_id"] = schoolId.ToString() },
				}
				: null,
		};

		var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);

		return session.Url ?? throw new InvalidOperationException("Stripe session URL is null");
	}

	/// <summary>
	/// Creates a Stripe billing portal session for the given school.
	/// </summary>
	public async Task<string> CreateBillingPortalSessionAsync(
		Guid schoolId,
		string returnUrl,
		CancellationToken cancellationToken = default)
	{
		var sub = await GetOrCreateAsync(schoolId, cancellationToken);

		if (sub.StripeCustomerId is null)
		{
			throw new InvalidOperationException("No Stripe customer for this school — subscribe first.");
		}

		var session = await billingPortalSessionService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
		{
			Customer = sub.StripeCustomerId,
			ReturnUrl = returnUrl,
		}, cancellationToken: cancellationToken);

		return session.Url ?? throw new InvalidOperationException("Stripe billing portal session URL is null");
	}

	/// <summary>
	/// Returns the active module names for the given school's subscription.
	/// During a valid trial, all modules are returned so schools can evaluate every feature.
	/// </summary>
	public async Task<IReadOnlyList<string>> GetActiveModulesAsync(Guid schoolId, CancellationToken cancellationToken = default)
	{
		var sub = await db.Subscriptions
			.Include(s => s.ActiveModules)
			.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken);

		if (sub is null)
		{
			return [];
		}

		var isTrialing = sub.Status == SubscriptionStatus.Trialing && sub.TrialEnd > DateTimeOffset.UtcNow;
		if (isTrialing)
		{
			return Enum.GetNames<SubscriptionModule>();
		}

		return sub.ActiveModules
			.Select(m => m.Module.ToString())
			.ToList();
	}

	/// <summary>
	/// Adds a module to the school's subscription via Stripe, then records it in DB.
	/// </summary>
	public async Task AddModuleAsync(Guid schoolId, SubscriptionModule module, CancellationToken cancellationToken = default)
	{
		var sub = await db.Subscriptions
			.Include(s => s.ActiveModules)
			.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken)
			?? throw new InvalidOperationException($"Subscription not found for school {schoolId}.");

		if (sub.Status != SubscriptionStatus.Active)
		{
			throw new InvalidOperationException("School does not have an active paid subscription. Modules can only be added to an active subscription.");
		}

		if (sub.StripeSubscriptionId is null)
		{
			throw new InvalidOperationException("School does not have an active Stripe subscription.");
		}

		if (!stripeOptions.Value.ModulePriceIds.TryGetValue(module.ToString(), out var intervalPrices)
			|| !intervalPrices.TryGetValue(sub.Interval.ToString(), out var priceId)
			|| string.IsNullOrEmpty(priceId))
		{
			throw new InvalidOperationException($"No Stripe price configured for module {module} ({sub.Interval}).");
		}

		if (sub.ActiveModules.Any(m => m.Module == module))
		{
			return;
		}

		var item = await subscriptionItemService.CreateAsync(new SubscriptionItemCreateOptions
		{
			Subscription = sub.StripeSubscriptionId,
			Price = priceId,
			Quantity = 1,
		}, cancellationToken: cancellationToken);

		db.SubscriptionModuleItems.Add(new SubscriptionModuleItem
		{
			Id = Guid.NewGuid(),
			SubscriptionId = sub.Id,
			Module = module,
			StripeSubscriptionItemId = item.Id,
		});

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex)
		{
			// Compensate: remove the Stripe item so billing matches DB state.
			logger.LogWarning(ex, "AddModuleAsync: DB save failed after Stripe item {ItemId} created for school {SchoolId}, module {Module}. Removing Stripe item.", item.Id, schoolId, module);
			try
			{
				await subscriptionItemService.DeleteAsync(item.Id, new SubscriptionItemDeleteOptions(), cancellationToken: cancellationToken);
			}
			catch (Exception stripeEx)
			{
				logger.LogError(stripeEx, "AddModuleAsync: compensation delete of Stripe item {ItemId} also failed — manual cleanup required.", item.Id);
			}

			throw;
		}
	}

	/// <summary>
	/// Removes a module from the school's subscription via Stripe, then removes it from DB.
	/// </summary>
	public async Task RemoveModuleAsync(Guid schoolId, SubscriptionModule module, CancellationToken cancellationToken = default)
	{
		var sub = await db.Subscriptions
			.Include(s => s.ActiveModules)
			.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken)
			?? throw new InvalidOperationException($"Subscription not found for school {schoolId}.");

		var moduleItem = sub.ActiveModules.FirstOrDefault(m => m.Module == module);
		if (moduleItem is null)
		{
			return;
		}

		var stripeItemId = (!moduleItem.IsAdminOverride) ? moduleItem.StripeSubscriptionItemId : null;

		db.SubscriptionModuleItems.Remove(moduleItem);
		await db.SaveChangesAsync(cancellationToken);

		if (stripeItemId is not null)
		{
			await subscriptionItemService.DeleteAsync(
				stripeItemId,
				new SubscriptionItemDeleteOptions(),
				cancellationToken: cancellationToken);
		}
	}

	/// <summary>
	/// Grants a module to a school via admin override — no Stripe charge.
	/// </summary>
	public async Task GrantModuleOverrideAsync(Guid schoolId, SubscriptionModule module, CancellationToken cancellationToken = default)
	{
		var sub = await db.Subscriptions
			.Include(s => s.ActiveModules)
			.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken)
			?? await GetOrCreateAsync(schoolId, cancellationToken);

		// Re-fetch with modules if GetOrCreateAsync was called
		if (!db.Entry(sub).Collection(s => s.ActiveModules).IsLoaded)
		{
			await db.Entry(sub).Collection(s => s.ActiveModules).LoadAsync(cancellationToken);
		}

		if (sub.ActiveModules.Any(m => m.Module == module))
		{
			return;
		}

		db.SubscriptionModuleItems.Add(new SubscriptionModuleItem
		{
			Id = Guid.NewGuid(),
			SubscriptionId = sub.Id,
			Module = module,
			IsAdminOverride = true,
		});

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			// Concurrent request inserted the same module — treat as no-op.
			var alreadyExists = await db.SubscriptionModuleItems
				.AnyAsync(m => m.SubscriptionId == sub.Id && m.Module == module, cancellationToken);
			if (!alreadyExists)
			{
				throw;
			}

			logger.LogDebug("GrantModuleOverrideAsync: duplicate insert for school {SchoolId}, module {Module} — ignored", schoolId, module);
		}
	}

	/// <summary>
	/// Processes a Stripe webhook event and updates the local subscription record.
	/// </summary>
	public async Task HandleWebhookAsync(Event stripeEvent, CancellationToken cancellationToken = default)
	{
		switch (stripeEvent.Type)
		{
			case EventTypes.CheckoutSessionCompleted:
				{
					if (stripeEvent.Data.Object is Session session)
					{
						await HandleCheckoutCompletedAsync(session, cancellationToken);
					}

					break;
				}

			case EventTypes.CustomerSubscriptionUpdated:
			case EventTypes.CustomerSubscriptionDeleted:
				{
					if (stripeEvent.Data.Object is StripeSubscription stripeSub)
					{
						await HandleSubscriptionChangedAsync(stripeSub, cancellationToken);
					}

					break;
				}

			case EventTypes.InvoicePaymentSucceeded:
				{
					if (stripeEvent.Data.Object is Invoice invoice)
					{
						await HandleInvoicePaymentSucceededAsync(invoice, cancellationToken);
					}

					break;
				}

			case EventTypes.InvoicePaymentFailed:
				{
					if (stripeEvent.Data.Object is Invoice failedInvoice)
					{
						await HandleInvoicePaymentFailedAsync(failedInvoice, cancellationToken);
					}

					break;
				}

			default:
				logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
				break;
		}
	}

	private async Task HandleCheckoutCompletedAsync(Session session, CancellationToken cancellationToken)
	{
		if (!session.Metadata.TryGetValue("school_id", out var schoolIdStr)
			|| !Guid.TryParse(schoolIdStr, out var schoolId))
		{
			logger.LogWarning("CheckoutSessionCompleted missing school_id metadata");
			return;
		}

		var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleCheckoutCompletedAsync: Subscription not found for SchoolId {SchoolId}, SessionId {SessionId}", schoolId, session.Id);
			return;
		}

		sub.StripeCustomerId ??= session.CustomerId;
		sub.StripeSubscriptionId = session.SubscriptionId;
		sub.Status = SubscriptionStatus.Active;

		if (session.Metadata.TryGetValue("interval", out var intervalStr)
			&& Enum.TryParse<BillingInterval>(intervalStr, out var interval))
		{
			sub.Interval = interval;
		}

		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);
	}

	private async Task HandleSubscriptionChangedAsync(StripeSubscription stripeSub, CancellationToken cancellationToken)
	{
		var sub = await db.Subscriptions.FirstOrDefaultAsync(
			s => s.StripeSubscriptionId == stripeSub.Id, cancellationToken);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleSubscriptionChangedAsync: Subscription not found for StripeSubscriptionId {StripeSubscriptionId}", stripeSub.Id);
			return;
		}

		var newStatus = stripeSub.Status switch
		{
			"active" => SubscriptionStatus.Active,
			"trialing" => SubscriptionStatus.Trialing,
			"past_due" => SubscriptionStatus.PastDue,
			"canceled" => SubscriptionStatus.Canceled,
			"unpaid" => SubscriptionStatus.Unpaid,
			_ => sub.Status,
		};

		// Never downgrade Active to Trialing via a subscription.updated event —
		// Stripe keeps the subscription in "trialing" state when adding items mid-trial,
		// which would undo the Active status written by checkout.session.completed.
		if (sub.Status == SubscriptionStatus.Active && newStatus == SubscriptionStatus.Trialing)
		{
			newStatus = SubscriptionStatus.Active;
		}

		sub.Status = newStatus;

		// Portal-driven monthly<->yearly plan switches arrive here as customer.subscription.updated.
		// Detect the new cadence from the base-plan line item specifically (matched by price ID) —
		// not "any recurring item" — since a module item could otherwise be mistaken for the base
		// plan and make later AddModuleAsync calls buy the wrong-interval module Price.
		var basePriceIds = stripeSub.Items?.Data?.Select(i => i.Price?.Id);
		if (basePriceIds?.Contains(stripeOptions.Value.BasePriceIdYearly) == true)
		{
			sub.Interval = BillingInterval.Yearly;
		}
		else if (basePriceIds?.Contains(stripeOptions.Value.BasePriceIdMonthly) == true)
		{
			sub.Interval = BillingInterval.Monthly;
		}
		else
		{
			logger.LogWarning("SubscriptionService.HandleSubscriptionChangedAsync: no base-plan price item found for StripeSubscriptionId {StripeSubscriptionId} — keeping existing Interval {Interval}", stripeSub.Id, sub.Interval);
		}

		// Stripe SDK returns DateTime.MinValue when unset — treat that as null
		sub.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd > DateTime.UnixEpoch
			? stripeSub.CurrentPeriodEnd
			: null;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);
	}

	private async Task HandleInvoicePaymentSucceededAsync(Invoice invoice, CancellationToken cancellationToken)
	{
		if (invoice.SubscriptionId is null)
		{
			return;
		}

		var sub = await db.Subscriptions.FirstOrDefaultAsync(
			s => s.StripeSubscriptionId == invoice.SubscriptionId, cancellationToken);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleInvoicePaymentSucceededAsync: Subscription not found for StripeSubscriptionId {StripeSubscriptionId}, InvoiceId {InvoiceId}", invoice.SubscriptionId, invoice.Id);
			return;
		}

		sub.Status = SubscriptionStatus.Active;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);
	}

	private async Task HandleInvoicePaymentFailedAsync(Invoice invoice, CancellationToken cancellationToken)
	{
		if (invoice.SubscriptionId is null)
		{
			return;
		}

		var sub = await db.Subscriptions.FirstOrDefaultAsync(
			s => s.StripeSubscriptionId == invoice.SubscriptionId, cancellationToken);
		if (sub is null)
		{
			logger.LogWarning("SubscriptionService.HandleInvoicePaymentFailedAsync: Subscription not found for StripeSubscriptionId {StripeSubscriptionId}, InvoiceId {InvoiceId}", invoice.SubscriptionId, invoice.Id);
			return;
		}

		sub.Status = SubscriptionStatus.PastDue;
		sub.UpdatedAt = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);
	}
}
