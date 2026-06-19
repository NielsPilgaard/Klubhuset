using Microsoft.AspNetCore.RateLimiting;

namespace Skoleoverblikket.Api;

public static class RateLimitingExtensions
{
	public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
	{
		services.AddRateLimiter(options =>
		{
			options.AddFixedWindowLimiter("demo-request", options =>
			{
				options.PermitLimit = 5;
				options.Window = TimeSpan.FromMinutes(15);
				options.QueueLimit = 0;
			});
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
		});

		return services;
	}
}
