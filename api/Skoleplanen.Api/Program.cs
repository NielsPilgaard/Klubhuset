using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Email;
using Skoleplanen.Api.Services;
using Skoleplanen.Api.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Multi-tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

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

// Conflict detection
builder.Services.AddScoped<ConflictDetectionService>();

// Controllers
builder.Services.AddControllers();

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
