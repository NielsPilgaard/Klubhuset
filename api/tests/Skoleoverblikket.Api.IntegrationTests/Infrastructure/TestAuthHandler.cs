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
        // X-Test-Roles: comma-separated role list; defaults to "admin"
        var rolesHeader = Request.Headers["X-Test-Roles"].FirstOrDefault();
        var roles = string.IsNullOrWhiteSpace(rolesHeader)
            ? ["admin"]
            : rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // X-Test-Subject: keycloak subject for /me lookups; defaults to "test-user-id"
        var subject = Request.Headers["X-Test-Subject"].FirstOrDefault() ?? "test-user-id";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.NameIdentifier, subject),
            new("sub", subject),
            new("tenant_id", TestTenantContext.DefaultTenantId.ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
