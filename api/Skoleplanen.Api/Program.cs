using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddDbContext<AppDbContext>(options =>
												options.UseNpgsql(
													builder.Configuration.GetConnectionString("skoleplanen-db")));

// Auth — validates Keycloak-issued JWTs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	   .AddJwtBearer(options =>
	   {
		   options.Authority = builder.Configuration["Keycloak:Authority"];
		   options.Audience = builder.Configuration["Keycloak:Audience"];
		   options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
		   // Preserve Keycloak's original claim names (e.g. "preferred_username", "name")
		   // instead of mapping them to WS-Federation URIs.
		   options.MapInboundClaims = false;

		   // Allow API to reach Keycloak internally (container-to-container) while
		   // still validating tokens issued by the public issuer URL.
		   var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
		   if (!string.IsNullOrEmpty(metadataAddress))
		   {
			   options.MetadataAddress = metadataAddress;
		   }
	   });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformer>();

builder.Services.AddOpenApi();

builder.Services.AddOptions<SmtpOptions>()
	   .BindConfiguration(SmtpOptions.SectionName)
	   .ValidateDataAnnotations()
	   .ValidateOnStart();

builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();

builder.Services.AddObjectStorage();

builder.Services.AddScoped<ConflictDetectionService>();

builder.Services.AddScoped<StaffInvitationService>();

builder.Services.AddScoped<Skoleplanen.Api.Services.SubscriptionService>();

// Configure Stripe global API key
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
{
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
}

builder.Services.AddControllers()
	   .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseSwaggerInDevelopment();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed well-known dev/prod fixtures (idempotent — skipped if already present).
// Skip when no connection string is present (e.g. swagger CLI running at build time).
if (!string.IsNullOrEmpty(app.Configuration.GetConnectionString("skoleplanen-db")))
{
	await app.Services.SeedAsync();
}

// Skip when S3 config is absent (e.g. swagger CLI running at build time).
if (!string.IsNullOrEmpty(app.Configuration["ObjectStorage:ServiceUrl"]))
{
	await app.Services.EnsureS3BucketAsync();
}

app.Run();
