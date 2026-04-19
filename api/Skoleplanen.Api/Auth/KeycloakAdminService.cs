using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Skoleplanen.Api.Auth;

public record CredentialRepresentation(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);

/// <summary>
/// Talks to the Keycloak Admin REST API to create users on behalf of the application.
/// Uses client_credentials flow with the skoleplanen-admin service account.
/// </summary>
public sealed class KeycloakAdminService(IKeycloakAdminApi adminApi, IKeycloakTokenApi tokenApi, IOptions<KeycloakOptions> options, ILogger<KeycloakAdminService> logger)
{
    /// <summary>
    /// Creates a Keycloak user, assigns the admin realm role, and returns the new user's Keycloak subject (UUID).
    /// </summary>
    public async Task<string> CreateAdminUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        CancellationToken ct)
    {
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

    /// <summary>
    /// Exchanges user credentials for a token via the password grant on the web client.
    /// Used immediately after signup so the frontend gets a JWT with tenant_id already embedded.
    /// </summary>
    public async Task<TokenResponse> GetTokenForUserAsync(string email, string password, CancellationToken ct)
    {
        var request = new PasswordTokenRequest(
            GrantType: "password",
            ClientId: options.Value.WebClientId,
            Username: email,
            Password: password,
            Scope: "openid profile roles tenant");

        return await tokenApi.GetPasswordTokenAsync(request, ct);
    }

    private async Task AssignRealmRoleAsync(string userId, string roleName, CancellationToken ct)
    {
        try
        {
            var role = await adminApi.GetRoleAsync(roleName, ct);
            await adminApi.AssignRoleMappingsAsync(userId, [role], ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assign role {RoleName} to user {UserId}", roleName, userId);
        }
    }
}

public sealed class KeycloakException(string message) : Exception(message);
