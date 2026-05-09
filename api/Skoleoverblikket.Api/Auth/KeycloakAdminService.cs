using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Skoleoverblikket.Api.Auth;

public record CredentialRepresentation(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);

/// <summary>
/// Talks to the Keycloak Admin REST API to create users on behalf of the application.
/// Uses client_credentials flow with the skoleoverblikket-admin service account.
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
            EmailVerified: true,
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
    /// Creates a Keycloak user for an invited staff member with no password and UPDATE_PASSWORD required action.
    /// Returns the new user's Keycloak subject (UUID).
    /// </summary>
    public async Task<string> CreateStaffUserAsync(
        string email,
        string firstName,
        string lastName,
        CancellationToken ct)
    {
        var payload = new CreateUserRequest(
            Username: email,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            Enabled: true,
            EmailVerified: true,
            Credentials: [],
            Attributes: [],
            RequiredActions: ["UPDATE_PASSWORD"]);

        var createResponse = await adminApi.CreateUserAsync(payload, ct);

        if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var existing = await adminApi.GetUsersByEmailAsync(email, exact: true, ct);
            return existing.FirstOrDefault()?.Id
                ?? throw new KeycloakException($"Keycloak rejected duplicate user but no existing user found for {email}");
        }

        if (!createResponse.IsSuccessStatusCode)
        {
            var err = await createResponse.Content.ReadAsStringAsync(ct);
            throw new KeycloakException($"Failed to create Keycloak staff user: {createResponse.StatusCode} — {err}");
        }

        var location = createResponse.Headers.Location
                       ?? throw new KeycloakException("Keycloak did not return a Location header after user creation");

        return location.Segments.Last().TrimEnd('/');
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

    /// <summary>
    /// Assigns or removes the Keycloak 'admin' realm role for an existing user.
    /// Throws <see cref="KeycloakException"/> on failure so callers can roll back DB changes.
    /// </summary>
    public async Task SetAdminRoleAsync(string keycloakUserId, bool grant, CancellationToken ct)
    {
        RoleRepresentation role;
        try
        {
            role = await adminApi.GetRoleAsync("admin", ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakException($"Failed to fetch 'admin' role from Keycloak: {ex.Message}");
        }

        try
        {
            if (grant)
            {
                await adminApi.AssignRoleMappingsAsync(keycloakUserId, [role], ct);
            }
            else
            {
                await adminApi.RemoveRoleMappingsAsync(keycloakUserId, [role], ct);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakException($"Failed to {(grant ? "assign" : "remove")} 'admin' role for user {keycloakUserId}: {ex.Message}");
        }
    }

    public async Task DeleteStaffUserAsync(string keycloakUserId, CancellationToken ct)
    {
        var response = await adminApi.DeleteUserAsync(keycloakUserId, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new KeycloakException($"Failed to delete Keycloak user {keycloakUserId}: {response.StatusCode} — {err}");
        }
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
