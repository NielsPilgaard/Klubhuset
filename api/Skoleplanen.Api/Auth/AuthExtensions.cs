using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Skoleplanen.Api.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.Authority = configuration["Keycloak:Authority"];
                   options.Audience = configuration["Keycloak:Audience"];
                   options.RequireHttpsMetadata = !environment.IsDevelopment();
                   // Preserve Keycloak's original claim names (e.g. "preferred_username", "name")
                   // instead of mapping them to WS-Federation URIs.
                   options.MapInboundClaims = false;

                   // Allow API to reach Keycloak internally (container-to-container) while
                   // still validating tokens issued by the public issuer URL.
                   var metadataAddress = configuration["Keycloak:MetadataAddress"];
                   if (!string.IsNullOrEmpty(metadataAddress))
                   {
                       options.MetadataAddress = metadataAddress;
                   }
               });

        services.AddAuthorization();
        services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformer>();

        return services;
    }
}
