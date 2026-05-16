namespace Skoleoverblikket.Api.Tenancy;

public static class TenancyExtensions
{
	public static IServiceCollection AddTenancy(this IServiceCollection services)
	{
		services.AddHttpContextAccessor();
		services.AddScoped<ITenantContext, HttpTenantContext>();
		services.AddExceptionHandler<MissingTenantClaimExceptionHandler>();

		return services;
	}
}
