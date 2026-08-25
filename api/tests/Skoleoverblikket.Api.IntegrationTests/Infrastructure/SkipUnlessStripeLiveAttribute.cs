using TUnit.Core;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Skips the decorated test unless STRIPE_LIVE_TEST=1 is set — see <see cref="RealStripeApiFactory"/>.
/// </summary>
public sealed class SkipUnlessStripeLiveAttribute() : SkipAttribute("STRIPE_LIVE_TEST=1 not set — skipping real-Stripe billing-cycle test")
{
	public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
		Task.FromResult(!RealStripeApiFactory.IsAvailable);
}
