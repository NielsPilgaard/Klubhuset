using System.Text.Json.Serialization;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Email;
using Skoleplanen.Api.Models;
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

		   // Allow API to reach Keycloak internally (container-to-container) while
		   // still validating tokens issued by the public issuer URL.
		   var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
		   if (!string.IsNullOrEmpty(metadataAddress))
		   {
			   options.MetadataAddress = metadataAddress;
		   }
	   });

builder.Services.AddAuthorization();

// OpenAPI / Swagger (spec generated from code)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	const string schemeId = "Bearer";

	options.SwaggerDoc("v1", new OpenApiInfo { Title = "Skoleplanen API", Version = "v1" });
	options.AddSecurityDefinition(schemeId,
								  new OpenApiSecurityScheme
								  {
									  Name = "Authorization",
									  Type = SecuritySchemeType.Http,
									  Scheme = schemeId,
									  BearerFormat = "JWT",
									  In = ParameterLocation.Header,
								  });

	options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
	{ [new OpenApiSecuritySchemeReference(schemeId, document)] = [] });
});

// Email
builder.Services.AddOptions<SmtpOptions>()
	   .BindConfiguration(SmtpOptions.SectionName)
	   .ValidateDataAnnotations()
	   .ValidateOnStart();

builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();

// Object storage (OVHCloud S3-compatible / LocalStack in dev)
builder.Services.AddOptions<S3Options>()
	   .BindConfiguration(S3Options.SectionName)
	   .ValidateDataAnnotations();

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
	var opts = sp.GetRequiredService<IOptions<S3Options>>().Value;
	var config = new AmazonS3Config
	{
		ServiceURL = opts.ServiceUrl,
		ForcePathStyle = true,
		AuthenticationRegion = RegionEndpoint.EUWest1.SystemName,
	};

	return new AmazonS3Client(new BasicAWSCredentials(opts.AccessKey, opts.SecretKey), config);
});

builder.Services.AddScoped<IObjectStorage, S3ObjectStorage>();

// Conflict detection
builder.Services.AddScoped<ConflictDetectionService>();

// Staff invitations
builder.Services.AddScoped<StaffInvitationService>();

// Controllers
builder.Services.AddControllers()
	   .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger(c => c.RouteTemplate = "api/v1/openapi/{documentName}/openapi.json");
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/api/v1/openapi/v1/openapi.json", "Skoleplanen API v1");
		c.RoutePrefix = "api/v1/openapi";
	});
}

app.UseMiddleware<SlugResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Dev seed — insert the test school if it doesn't exist yet.
// Skip when no connection string is present (e.g. swagger CLI running at build time).
var connectionString = app.Configuration.GetConnectionString("skoleplanen-db");
if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(connectionString))
{
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await db.Database.MigrateAsync();
	var testSchoolId = new Guid("11111111-1111-1111-1111-111111111111");
	var exists = await db.Schools.IgnoreQueryFilters().AnyAsync(s => s.Id == testSchoolId);
	if (!exists)
	{
		db.Schools.Add(new School
		{
			Id = testSchoolId,
			Name = "Testskole",
			Slug = "testskole",
			ContactEmail = "admin@testskole.dk"
		});

		await db.SaveChangesAsync();
	}
}

app.Run();
