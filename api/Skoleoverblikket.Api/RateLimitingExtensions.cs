using System.Threading.RateLimiting;

namespace Skoleoverblikket.Api;

public static class RateLimitingExtensions
{
	public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
	{
		services.AddRateLimiter(options =>
		{
			options.AddPolicy("demo-request", context =>
				RateLimitPartition.GetFixedWindowLimiter(
					partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
					factory: _ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 5,
						Window = TimeSpan.FromMinutes(15),
						QueueLimit = 0,
					}));
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
		});

		return services;
	}
}
