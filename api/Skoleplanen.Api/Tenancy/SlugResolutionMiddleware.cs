using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Skoleplanen.Api.Data;

namespace Skoleplanen.Api.Tenancy;

/// <summary>
/// Extracts the tenant slug from the URL path prefix (/{slug}/...) and resolves
/// it to a TenantId via a cached DB lookup. Injects the resolved TenantId into
/// the request Items collection so downstream <see cref="HttpTenantContext"/> can read it.
///
/// Returns HTTP 404 for unknown slugs. Paths that don't start with a known-slug
/// segment (e.g. /api/v1/tenants) are left untouched.
/// </summary>
public sealed class SlugResolutionMiddleware(RequestDelegate next, IMemoryCache cache)
{
	// Paths that bypass slug resolution entirely
	private static readonly HashSet<string> BypassPrefixes =
	[
		"/api/v1/tenants",
		"/api/v1/openapi",
		"/health",
	];

	public const string TenantIdItemKey = "ResolvedTenantId";

	public async Task InvokeAsync(HttpContext context, AppDbContext db)
	{
		var path = context.Request.Path.Value ?? "/";

		// Skip bypass paths
		if (BypassPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
		{
			await next(context);
			return;
		}

		// Paths under /{slug}/... — extract the first segment
		var segments = path.TrimStart('/').Split('/', 2);
		var candidate = segments[0];

		if (string.IsNullOrEmpty(candidate) || !IsValidSlugFormat(candidate))
		{
			// No slug prefix — pass through (JWT-based tenancy still applies)
			await next(context);
			return;
		}

		var tenantId = await ResolveSlugAsync(candidate, db, context.RequestAborted);
		if (tenantId is null)
		{
			context.Response.StatusCode = StatusCodes.Status404NotFound;
			await context.Response.WriteAsJsonAsync(new { title = "Tenant ikke fundet", status = 404 });
			return;
		}

		context.Items[TenantIdItemKey] = tenantId.Value;
		await next(context);
	}

	private async Task<Guid?> ResolveSlugAsync(string slug, AppDbContext db, CancellationToken ct)
	{
		var cacheKey = $"slug:{slug}";

		if (cache.TryGetValue(cacheKey, out Guid cached))
		{
			// Guid.Empty is used as a sentinel for "not found" (negative cache)
			return cached == Guid.Empty ? null : cached;
		}

		var school = await db.Schools
							 .AsNoTracking()
							 .IgnoreQueryFilters()
							 .Where(s => s.Slug == slug)
							 .Select(s => new { s.Id })
							 .FirstOrDefaultAsync(ct);

		if (school is null)
		{
			// Cache the miss as Guid.Empty (negative cache) for 10 minutes
			cache.Set(cacheKey, Guid.Empty, TimeSpan.FromMinutes(10));
			return null;
		}

		cache.Set(cacheKey, school.Id, TimeSpan.FromMinutes(10));
		return school.Id;
	}

	private static bool IsValidSlugFormat(string s) =>
		s.Length is >= 3 and <= 40 &&
		s.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
}
