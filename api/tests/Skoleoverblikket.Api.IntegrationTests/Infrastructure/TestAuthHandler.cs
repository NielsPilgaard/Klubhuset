using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Skoleoverblikket.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Fake authentication handler used in tests. Always returns an authenticated
/// principal so endpoints don't 401. The TenantId is provided via
/// <see cref="TestTenantContext"/> (injected into AppDbContext), not via claims.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("tenant_id", TestTenantContext.DefaultTenantId.ToString()),
            // Default role: admin — individual tests can override if needed
            new Claim(ClaimTypes.Role, "admin"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
