using ZiggyCreatures.Caching.Fusion;

namespace Skoleoverblikket.Api.Cache;

public static class CacheExtensions
{
	public static IServiceCollection AddFusionCacheDefaults(this IServiceCollection services)
	{
		services.AddMemoryCache();
		services.AddFusionCache()
			.WithDefaultEntryOptions(o =>
			{
				o.Duration = TimeSpan.FromMinutes(5);
				o.IsFailSafeEnabled = true;
				o.FailSafeMaxDuration = TimeSpan.FromHours(1);
				o.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);
				o.EagerRefreshThreshold = 0.9f;
				o.FactorySoftTimeout = TimeSpan.FromSeconds(1);
				o.FactoryHardTimeout = TimeSpan.FromSeconds(5);
			});

		return services;
	}
}
