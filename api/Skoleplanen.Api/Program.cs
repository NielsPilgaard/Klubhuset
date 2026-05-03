using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Refit;
using Skoleplanen.Api;
using Skoleplanen.Api.Auth;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Email;
using Skoleplanen.Api.OpenApi;
using Skoleplanen.Api.Services;
using Skoleplanen.Api.Storage;
using Skoleplanen.Api.Tenancy;
using Skoleplanen.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Multi-tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddMemoryCache();

// Database
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("skoleplanen-db")));

// Keycloak
builder.Services.AddOptions<KeycloakOptions>()
       .BindConfiguration(KeycloakOptions.SectionName)
       .ValidateDataAnnotations()
       .ValidateOnStart();

// Auth — validates Keycloak-issued JWTs
builder.Services.AddKeycloakAuth();

// Keycloak Admin REST API clients (for creating users during signup)
builder.Services
    .AddRefitClient<IKeycloakTokenApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var kc = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        c.BaseAddress = new Uri(kc.TokenBaseUrl);
    });

builder.Services
    .AddTransient<KeycloakBearerHandler>()
    .AddRefitClient<IKeycloakAdminApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var kc = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        c.BaseAddress = new Uri(kc.AdminBaseUrl);
    })
    .AddHttpMessageHandler<KeycloakBearerHandler>();

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

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MissingTenantClaimExceptionHandler>();

builder.Services.AddControllers()
	   .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseSwaggerInDevelopment();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

var isOpenApiGeneration = string.Equals(Environment.GetEnvironmentVariable("OPENAPI_GENERATE"), "true", StringComparison.OrdinalIgnoreCase);
if (!app.Environment.IsProduction() && !isOpenApiGeneration)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsEnvironment("Testing"))
{
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
