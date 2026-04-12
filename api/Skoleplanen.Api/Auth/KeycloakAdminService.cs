using System.Text.Json.Serialization;

namespace Skoleplanen.Api.Auth;

public record CredentialRepresentation(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);

/// <summary>
/// Talks to the Keycloak Admin REST API to create users on behalf of the application.
/// Uses client_credentials flow with the skoleplanen-admin service account.
/// </summary>
public sealed class KeycloakAdminService(IKeycloakAdminApi adminApi)
{
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
