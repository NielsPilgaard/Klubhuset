using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Skoleplanen.Api.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   // Resolve options at configuration time via IOptions<KeycloakOptions>
               });

        // Configure JwtBearerOptions from KeycloakOptions after the options graph is built
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<KeycloakOptions>>((jwt, kc) =>
                {
                    jwt.Authority = kc.Value.Authority;
                    jwt.Audience = kc.Value.Audience;
                    jwt.RequireHttpsMetadata = !environment.IsDevelopment();
                    jwt.MapInboundClaims = false;

                    if (!string.IsNullOrEmpty(kc.Value.MetadataAddress))
                        jwt.MetadataAddress = kc.Value.MetadataAddress;
                });

        services.AddAuthorization();
        services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformer>();

        return services;
    }
}
