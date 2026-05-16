using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Cache;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Email;
using Skoleoverblikket.Api.OpenApi;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Storage;
using Skoleoverblikket.Api.Tenancy;
using Skoleoverblikket.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddTenancy();
builder.Services.AddFusionCacheDefaults();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddKeycloakAuth();
builder.Services.AddKeycloakAdmin();
builder.Services.AddOpenApi();
builder.Services.AddEmail();
builder.Services.AddObjectStorage();
builder.Services.AddStripe(builder.Configuration);
builder.Services.AddDomainServices();

var app = builder.Build();

app.UseSwaggerInDevelopment();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

await app.MigrateAndSeedAsync();

app.Run();
