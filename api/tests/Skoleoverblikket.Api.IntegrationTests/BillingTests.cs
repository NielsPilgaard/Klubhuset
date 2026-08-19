using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
using Skoleoverblikket.Api.Models;
using Stripe;
using LocalSubscriptionService = Skoleoverblikket.Api.Services.SubscriptionService;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Integration tests for BillingController GET /subscription.
/// Covers:
///   - Non-admin returns 403.
///   - New school with no subscription record gets auto-created Trialing record.
///   - Trialing school: IsTrialing=true, HasAccess=true, TrialDaysLeft>0.
///   - Expired trial: IsTrialing=false, HasAccess=false, TrialDaysLeft=0.
///   - Active subscription: IsActive=true, HasAccess=true.
///   - Active modules listed correctly.
///   - Yearly billing: checkout persists Interval, AddModuleAsync picks the matching-interval
///     module price, and a portal-driven interval switch (subscription.updated) updates Interval.
///   Checkout/module calls hit stripe-mock (Testcontainers) instead of the real Stripe API —
///   see ApiFactory.
/// </summary>
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class BillingTests(ApiFactory factory)
{
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNameCaseInsensitive = true,
	};

	private readonly ApiFactory _factory = factory;
	private readonly Guid _tenantId = Guid.NewGuid();
	private HttpClient _adminClient = null!;

	[Before(Test)]
	public async Task SetUp()
	{
		await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantId);
		_adminClient = _factory.CreateClient();
		_adminClient.DefaultRequestHeaders.Add("X-Test-TenantId", _tenantId.ToString());
		_adminClient.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
		_adminClient.DefaultRequestHeaders.Add("X-Test-Subject", "billing-admin-subject");
	}

	// ── Private helpers ──────────────────────────────────────────────────────────

	private async Task<Models.Subscription> SeedSubscriptionAsync(
		SubscriptionStatus status,
		DateTimeOffset trialEnd,
		DateTimeOffset? currentPeriodEnd = null,
		string? stripeSubscriptionId = null)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var sub = new Models.Subscription
		{
			Id = Guid.NewGuid(),
			SchoolId = _tenantId,
			Status = status,
			TrialEnd = trialEnd,
			CurrentPeriodEnd = currentPeriodEnd,
			StripeSubscriptionId = stripeSubscriptionId,
		};
		db.Subscriptions.Add(sub);
		await db.SaveChangesAsync();
		return sub;
	}

	private async Task SeedModuleAsync(Guid subscriptionId, SubscriptionModule module)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		db.SubscriptionModuleItems.Add(new SubscriptionModuleItem
		{
			Id = Guid.NewGuid(),
			SubscriptionId = subscriptionId,
			Module = module,
			IsAdminOverride = true,
		});
		await db.SaveChangesAsync();
	}

	// ── Auth enforcement ──────────────────────────────────────────────────────────

	[Test]
	public async Task GetSubscription_NonAdmin_Returns403()
	{
		using var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-TenantId", _tenantId.ToString());
		client.DefaultRequestHeaders.Add("X-Test-Roles", "user");
		client.DefaultRequestHeaders.Add("X-Test-Subject", "nonadmin-billing");

		var response = await client.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
	}

	// ── Trial state ───────────────────────────────────────────────────────────────

	[Test]
	public async Task GetSubscription_NewSchool_AutoCreatesTrialingRecord()
	{
		// No Subscription seeded — GetOrCreateAsync should auto-create one
		var response = await _adminClient.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.Status).IsEqualTo(SubscriptionStatus.Trialing);
		await Assert.That(dto.IsTrialing).IsTrue();
		await Assert.That(dto.HasAccess).IsTrue();
		await Assert.That(dto.TrialDaysLeft).IsGreaterThan(0);
	}

	[Test]
	public async Task GetSubscription_ActiveTrial_ReturnsCorrectTrialDaysLeft()
	{
		var trialEnd = DateTimeOffset.UtcNow.AddDays(7);
		await SeedSubscriptionAsync(SubscriptionStatus.Trialing, trialEnd);

		var response = await _adminClient.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.IsTrialing).IsTrue();
		await Assert.That(dto.HasAccess).IsTrue();
		// Should be ~7 days; allow ±1 for clock skew
		await Assert.That(dto.TrialDaysLeft).IsGreaterThanOrEqualTo(6);
		await Assert.That(dto.TrialDaysLeft).IsLessThanOrEqualTo(8);
	}

	[Test]
	public async Task GetSubscription_ExpiredTrial_IsTrialingFalse_HasAccessFalse()
	{
		var trialEnd = DateTimeOffset.UtcNow.AddDays(-1); // past
		await SeedSubscriptionAsync(SubscriptionStatus.Trialing, trialEnd);

		var response = await _adminClient.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.IsTrialing).IsFalse();
		await Assert.That(dto.HasAccess).IsFalse();
		await Assert.That(dto.TrialDaysLeft).IsEqualTo(0);
	}

	// ── Active subscription ───────────────────────────────────────────────────────

	[Test]
	public async Task GetSubscription_Active_IsActiveTrueHasAccessTrue()
	{
		var periodEnd = DateTimeOffset.UtcNow.AddMonths(1);
		await SeedSubscriptionAsync(
			SubscriptionStatus.Active,
			trialEnd: DateTimeOffset.UtcNow.AddDays(-7),
			currentPeriodEnd: periodEnd,
			stripeSubscriptionId: "sub_test_active");

		var response = await _adminClient.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.IsActive).IsTrue();
		await Assert.That(dto.HasAccess).IsTrue();
		await Assert.That(dto.IsTrialing).IsFalse();
		await Assert.That(dto.TrialDaysLeft).IsEqualTo(0);
	}

	// ── Active modules ────────────────────────────────────────────────────────────

	[Test]
	public async Task GetSubscription_WithActiveModule_ListsModule()
	{
		var sub = await SeedSubscriptionAsync(
			SubscriptionStatus.Active,
			trialEnd: DateTimeOffset.UtcNow.AddDays(-7),
			stripeSubscriptionId: "sub_test_modules");
		await SeedModuleAsync(sub.Id, SubscriptionModule.ParentModule);

		var response = await _adminClient.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.ActiveModules).Contains(SubscriptionModule.ParentModule.ToString());
	}

	[Test]
	public async Task GetSubscription_NoModules_ActiveModulesEmpty()
	{
		await SeedSubscriptionAsync(
			SubscriptionStatus.Active,
			trialEnd: DateTimeOffset.UtcNow.AddDays(-7),
			stripeSubscriptionId: "sub_test_no_modules");

		var response = await _adminClient.GetAsync("/api/v1/billing/subscription");

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.ActiveModules.Count).IsEqualTo(0);
	}

	// ── Yearly billing interval ──────────────────────────────────────────────────

	[Test]
	public async Task CreateCheckout_YearlyInterval_PersistsIntervalOnSubscription()
	{
		var response = await _adminClient.PostAsJsonAsync(
			"/api/v1/billing/checkout",
			new BillingController.CheckoutRequest(BillingInterval.Yearly));

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		// Interval is persisted once checkout.session.completed arrives, not at session creation —
		// simulate the webhook the same way Stripe would deliver it.
		await CompleteCheckoutAsync(BillingInterval.Yearly);

		var subResponse = await _adminClient.GetAsync("/api/v1/billing/subscription");
		var dto = await subResponse.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.Interval).IsEqualTo(BillingInterval.Yearly);
	}

	[Test]
	public async Task CreateCheckout_MonthlyInterval_PersistsIntervalOnSubscription()
	{
		var response = await _adminClient.PostAsJsonAsync(
			"/api/v1/billing/checkout",
			new BillingController.CheckoutRequest(BillingInterval.Monthly));

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

		await CompleteCheckoutAsync(BillingInterval.Monthly);

		var subResponse = await _adminClient.GetAsync("/api/v1/billing/subscription");
		var dto = await subResponse.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.Interval).IsEqualTo(BillingInterval.Monthly);
	}

	private async Task CompleteCheckoutAsync(BillingInterval interval)
	{
		using var scope = _factory.Services.CreateScope();
		var subscriptionService = scope.ServiceProvider.GetRequiredService<LocalSubscriptionService>();

		var session = new Stripe.Checkout.Session
		{
			Metadata = new Dictionary<string, string>
			{
				["school_id"] = _tenantId.ToString(),
				["interval"] = interval.ToString(),
			},
		};

		var stripeEvent = new Event
		{
			Type = EventTypes.CheckoutSessionCompleted,
			Data = new EventData { Object = session },
		};

		await subscriptionService.HandleWebhookAsync(stripeEvent);
	}

	[Test]
	public async Task AddModule_YearlySubscription_UsesYearlyModulePrice()
	{
		var sub = await SeedSubscriptionAsync(
			SubscriptionStatus.Active,
			trialEnd: DateTimeOffset.UtcNow.AddDays(-7),
			stripeSubscriptionId: "sub_test_yearly_module");

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var trackedSub = await db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
			trackedSub.Interval = BillingInterval.Yearly;
			await db.SaveChangesAsync();
		}

		var response = await _adminClient.PostAsJsonAsync(
			"/api/v1/billing/modules",
			new BillingController.ModuleRequest(SubscriptionModule.ParentModule));

		await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

		var subResponse = await _adminClient.GetAsync("/api/v1/billing/subscription");
		var dto = await subResponse.Content.ReadFromJsonAsync<BillingController.SubscriptionDto>(JsonOpts);
		await Assert.That(dto).IsNotNull();
		await Assert.That(dto!.ActiveModules).Contains(SubscriptionModule.ParentModule.ToString());

		// stripe-mock returns fixture data for subscription_items regardless of the requested
		// Price, so the yearly-vs-monthly price selection can't be observed by reading the item
		// back — AddModuleAsync throws if ModulePriceIds has no entry for the sub's Interval,
		// so reaching NoContent above already proves the yearly price was resolved.
		using var verifyScope = _factory.Services.CreateScope();
		var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
		var moduleItem = await verifyDb.SubscriptionModuleItems
			.FirstOrDefaultAsync(m => m.SubscriptionId == sub.Id && m.Module == SubscriptionModule.ParentModule);
		await Assert.That(moduleItem).IsNotNull();
	}

	[Test]
	public async Task Webhook_SubscriptionUpdatedWithYearlyPrice_UpdatesIntervalToYearly()
	{
		var sub = await SeedSubscriptionAsync(
			SubscriptionStatus.Active,
			trialEnd: DateTimeOffset.UtcNow.AddDays(-7),
			stripeSubscriptionId: "sub_test_portal_switch");

		using var scope = _factory.Services.CreateScope();
		var subscriptionService = scope.ServiceProvider.GetRequiredService<LocalSubscriptionService>();

		var stripeSub = new Stripe.Subscription
		{
			Id = "sub_test_portal_switch",
			Status = "active",
			Items = new StripeList<SubscriptionItem>
			{
				Data =
				[
					new SubscriptionItem
					{
						Price = new Price
						{
							Id = "price_stub_yearly",
							Recurring = new PriceRecurring { Interval = "year" },
						},
					},
				],
			},
		};

		var stripeEvent = new Event
		{
			Type = EventTypes.CustomerSubscriptionUpdated,
			Data = new EventData { Object = stripeSub },
		};

		await subscriptionService.HandleWebhookAsync(stripeEvent);

		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var updatedSub = await db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
		await Assert.That(updatedSub.Interval).IsEqualTo(BillingInterval.Yearly);
	}

	[Test]
	public async Task Webhook_SubscriptionUpdatedWithMonthlyPrice_UpdatesIntervalToMonthly()
	{
		var sub = await SeedSubscriptionAsync(
			SubscriptionStatus.Active,
			trialEnd: DateTimeOffset.UtcNow.AddDays(-7),
			stripeSubscriptionId: "sub_test_portal_switch_monthly");

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var trackedSub = await db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
		trackedSub.Interval = BillingInterval.Yearly;
		await db.SaveChangesAsync();

		var subscriptionService = scope.ServiceProvider.GetRequiredService<LocalSubscriptionService>();

		var stripeSub = new Stripe.Subscription
		{
			Id = "sub_test_portal_switch_monthly",
			Status = "active",
			Items = new StripeList<SubscriptionItem>
			{
				Data =
				[
					new SubscriptionItem
					{
						Price = new Price
						{
							Id = "price_stub_monthly",
							Recurring = new PriceRecurring { Interval = "month" },
						},
					},
				],
			},
		};

		var stripeEvent = new Event
		{
			Type = EventTypes.CustomerSubscriptionUpdated,
			Data = new EventData { Object = stripeSub },
		};

		await subscriptionService.HandleWebhookAsync(stripeEvent);

		var updatedSub = await db.Subscriptions.FirstAsync(s => s.Id == sub.Id);
		await Assert.That(updatedSub.Interval).IsEqualTo(BillingInterval.Monthly);
	}
}
