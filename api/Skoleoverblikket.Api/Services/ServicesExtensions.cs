using System.Text.Json.Serialization;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Services;

public static class ServicesExtensions
{
	public static IServiceCollection AddDomainServices(this IServiceCollection services)
	{
		services.AddScoped<ConflictDetectionService>();
		services.AddScoped<StaffInvitationService>();
		services.AddScoped<ParentInvitationService>();
		services.AddScoped<ExcelReportBuilder>();
		services.AddScoped<SubscriptionService>();

		services.AddOptions<ApplicationOptions>()
			.BindConfiguration(ApplicationOptions.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddProblemDetails();

		services.AddControllers()
			.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

		return services;
	}
}
