using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Skoleplanen.Api.Auth;

/// <summary>
/// Talks to the Keycloak Admin REST API to create users on behalf of the application.
/// Uses client_credentials flow with the skoleplanen-admin service account.
/// </summary>
public sealed class KeycloakAdminService(IConfiguration config, IHttpClientFactory httpClientFactory)
{
    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);

    private record CreateUserRequest(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        bool EmailVerified,
        IReadOnlyList<CredentialRepresentation> Credentials,
        Dictionary<string, IReadOnlyList<string>> Attributes);

    private record CredentialRepresentation(
        string Type,
        string Value,
        bool Temporary);

    private string AuthorityBase =>
        (config["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority not configured"))
        .TrimEnd('/');

    // Derives the realm-admin base URL from the authority URL.
    // Authority: https://auth.example.com/realms/MyRealm
    // Admin base: https://auth.example.com/admin/realms/MyRealm
    private string AdminBase
    {
        get
        {
            var uri = new Uri(AuthorityBase);
            // path is /realms/RealmName
            var realmSegment = uri.AbsolutePath.TrimStart('/'); // "realms/RealmName"
            return $"{uri.Scheme}://{uri.Authority}/admin/{realmSegment}";
        }
    }

    private string TokenUrl => $"{AuthorityBase}/protocol/openid-connect/token";

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var clientId = config["Keycloak:AdminClientId"]
                       ?? throw new InvalidOperationException("Keycloak:AdminClientId not configured");
        var clientSecret = config["Keycloak:AdminClientSecret"]
                           ?? throw new InvalidOperationException("Keycloak:AdminClientSecret not configured");

        var client = httpClientFactory.CreateClient("keycloak-admin");
        var response = await client.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        }), ct);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var token = JsonSerializer.Deserialize<TokenResponse>(body)
                    ?? throw new InvalidOperationException("Invalid token response from Keycloak");
        return token.AccessToken;
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
        var client = httpClientFactory.CreateClient("keycloak-admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create the user
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

        var createResponse = await client.PostAsync(
            $"{AdminBase}/users",
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"),
            ct);

        if (!createResponse.IsSuccessStatusCode)
        {
            var err = await createResponse.Content.ReadAsStringAsync(ct);
            throw new KeycloakException($"Failed to create Keycloak user: {createResponse.StatusCode} — {err}");
        }

        // 2. Extract user ID from Location header
        var location = createResponse.Headers.Location
                       ?? throw new KeycloakException("Keycloak did not return a Location header after user creation");
        var keycloakUserId = location.Segments.Last().TrimEnd('/');

        // 3. Assign admin realm role
        await AssignRealmRoleAsync(client, keycloakUserId, "admin", ct);

        return keycloakUserId;
    }

    private async Task AssignRealmRoleAsync(HttpClient client, string userId, string roleName, CancellationToken ct)
    {
        // First fetch the role representation (we need id + name)
        var roleResponse = await client.GetAsync($"{AdminBase}/roles/{roleName}", ct);
        if (!roleResponse.IsSuccessStatusCode)
        {
            // Non-critical — user is created, just missing the role
            return;
        }

        var roleJson = await roleResponse.Content.ReadAsStringAsync(ct);

        // POST role assignment
        var assignResponse = await client.PostAsync(
            $"{AdminBase}/users/{userId}/role-mappings/realm",
            new StringContent($"[{roleJson}]", System.Text.Encoding.UTF8, "application/json"),
            ct);

        assignResponse.EnsureSuccessStatusCode();
    }
}

public sealed class KeycloakException(string message) : Exception(message);
