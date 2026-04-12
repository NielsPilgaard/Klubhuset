using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api;
using Skoleplanen.Api.Auth;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Email;
using Skoleplanen.Api.OpenApi;
using Skoleplanen.Api.Services;
using Skoleplanen.Api.Storage;
using Skoleplanen.Api.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Multi-tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddMemoryCache();

// Database
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("skoleplanen-db")));

// Auth — validates Keycloak-issued JWTs
builder.Services.AddKeycloakAuth(builder.Configuration, builder.Environment);

// Keycloak Admin REST API client (for creating users during signup)
builder.Services.AddHttpClient("keycloak-admin");
builder.Services.AddScoped<KeycloakAdminService>();

builder.Services.AddOpenApi();

builder.Services.AddOptions<ApplicationOptions>()
	   .BindConfiguration(ApplicationOptions.SectionName)
	   .ValidateDataAnnotations()
	   .ValidateOnStart();

builder.Services.AddOptions<SmtpOptions>()
	   .BindConfiguration(SmtpOptions.SectionName)
	   .ValidateDataAnnotations()
	   .ValidateOnStart();

builder.Services.AddOptions<StripeOptions>()
	   .BindConfiguration(StripeOptions.SectionName)
	   .ValidateDataAnnotations()
	   .ValidateOnStart();

builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();

builder.Services.AddObjectStorage();

builder.Services.AddScoped<ConflictDetectionService>();

builder.Services.AddScoped<StaffInvitationService>();
builder.Services.AddScoped<ExcelReportBuilder>();

builder.Services.AddScoped<SubscriptionService>();

builder.Services.AddStripe(builder.Configuration);

builder.Services.AddControllers()
	   .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseSwaggerInDevelopment();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var isOpenApiGeneration = string.Equals(Environment.GetEnvironmentVariable("OPENAPI_GENERATE"), "true", StringComparison.OrdinalIgnoreCase);
if (!isOpenApiGeneration)
{
	if (!builder.Environment.IsProduction())
	{
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
	}

	// Seed well-known dev/prod fixtures (idempotent — skipped if already present).
	if (!string.IsNullOrEmpty(app.Configuration.GetConnectionString("skoleplanen-db")))
	{
		_ = Task.Run(() => app.Services.SeedAsync());
	}

	if (!string.IsNullOrEmpty(app.Configuration["ObjectStorage:ServiceUrl"]))
	{
		_ = Task.Run(() => app.Services.EnsureS3BucketAsync());
	}
}

app.Run();
