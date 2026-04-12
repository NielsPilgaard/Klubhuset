using Microsoft.Extensions.Options;
using Refit;
using System.Text.Json.Serialization;

namespace Skoleplanen.Api.Auth;

/// <summary>Token endpoint — base URL: {authority}/protocol/openid-connect</summary>
public interface IKeycloakTokenApi
{
    [Post("/token")]
    Task<TokenResponse> GetTokenAsync([Body(BodySerializationMethod.UrlEncoded)] TokenRequest request, CancellationToken ct);
}

/// <summary>Admin REST API — base URL: {adminBase}</summary>
public interface IKeycloakAdminApi
{
    [Post("/users")]
    Task<HttpResponseMessage> CreateUserAsync([Body] CreateUserRequest request, CancellationToken ct);

    [Get("/roles/{roleName}")]
    Task<HttpResponseMessage> GetRoleAsync(string roleName, CancellationToken ct);

    [Post("/users/{userId}/role-mappings/realm")]
    Task AssignRoleMappingsAsync(string userId, [Body] string roleJson, CancellationToken ct);
}

public record TokenRequest(
    [property: AliasAs("grant_type")] string GrantType,
    [property: AliasAs("client_id")] string ClientId,
    [property: AliasAs("client_secret")] string ClientSecret);

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken);

public record CreateUserRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("emailVerified")] bool EmailVerified,
    [property: JsonPropertyName("credentials")] IReadOnlyList<CredentialRepresentation> Credentials,
    [property: JsonPropertyName("attributes")] Dictionary<string, IReadOnlyList<string>> Attributes);

public record CredentialRepresentation(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Talks to the Keycloak Admin REST API to create users on behalf of the application.
/// Uses client_credentials flow with the skoleplanen-admin service account.
/// </summary>
public sealed class KeycloakAdminService(
    IOptions<KeycloakOptions> options,
    IKeycloakTokenApi tokenApi,
    IKeycloakAdminApi adminApi)
{
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var kc = options.Value;
        var response = await tokenApi.GetTokenAsync(
            new TokenRequest("client_credentials", kc.AdminClientId, kc.AdminClientSecret), ct);
        return response.AccessToken;
    }

    /// <summary>
    /// Creates a Keycloak user and returns the new user's Keycloak subject (UUID).
    /// Sets the tenant_id attribute and assigns the admin realm role.
    /// </summary>
    public async Task<string> CreateAdminUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);

        // Attach the bearer token for admin calls via the DelegatingHandler set up in Program.cs
        using var scope = new BearerTokenScope(token);

        var payload = new CreateUserRequest(
            Username: email,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            Enabled: true,
            EmailVerified: false,
            Credentials: [new CredentialRepresentation("password", password, Temporary: false)],
            Attributes: new Dictionary<string, IReadOnlyList<string>>
            {
                ["tenant_id"] = [tenantId.ToString()],
            });

        var createResponse = await adminApi.CreateUserAsync(payload, ct);

        if (!createResponse.IsSuccessStatusCode)
        {
            var err = await createResponse.Content.ReadAsStringAsync(ct);
            throw new KeycloakException($"Failed to create Keycloak user: {createResponse.StatusCode} — {err}");
        }

        var location = createResponse.Headers.Location
                       ?? throw new KeycloakException("Keycloak did not return a Location header after user creation");
        var keycloakUserId = location.Segments.Last().TrimEnd('/');

        await AssignRealmRoleAsync(keycloakUserId, "admin", ct);

        return keycloakUserId;
    }

    private async Task AssignRealmRoleAsync(string userId, string roleName, CancellationToken ct)
    {
        var roleResponse = await adminApi.GetRoleAsync(roleName, ct);
        if (!roleResponse.IsSuccessStatusCode)
        {
            // Non-critical — user is created, just missing the role
            return;
        }

        var roleJson = await roleResponse.Content.ReadAsStringAsync(ct);
        await adminApi.AssignRoleMappingsAsync(userId, $"[{roleJson}]", ct);
    }
}

public sealed class KeycloakException(string message) : Exception(message);

// ── Bearer token ambient scope (thread-static) ────────────────────────────────

/// <summary>
/// Stores the bearer token for the current async operation so the
/// <see cref="KeycloakBearerHandler"/> can attach it without coupling
/// the service to HttpClient internals.
/// </summary>
internal static class KeycloakBearerContext
{
    [ThreadStatic]
    private static string? _token;

    internal static string? Token => _token;

    internal static IDisposable Set(string token)
    {
        _token = token;
        return new Cleanup();
    }

    private sealed class Cleanup : IDisposable
    {
        public void Dispose() => _token = null;
    }
}

internal sealed class BearerTokenScope : IDisposable
{
    private readonly IDisposable _cleanup;
    public BearerTokenScope(string token) => _cleanup = KeycloakBearerContext.Set(token);
    public void Dispose() => _cleanup.Dispose();
}

/// <summary>
/// DelegatingHandler that picks up the ambient bearer token and injects it
/// as an Authorization header for outgoing Keycloak Admin API requests.
/// </summary>
public sealed class KeycloakBearerHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = KeycloakBearerContext.Token;
        if (token is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, ct);
    }
}
