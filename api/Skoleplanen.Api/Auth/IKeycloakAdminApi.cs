using System.Text.Json.Serialization;
using Refit;

namespace Skoleplanen.Api.Auth;

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

public record CreateUserRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("emailVerified")] bool EmailVerified,
    [property: JsonPropertyName("credentials")] IReadOnlyList<CredentialRepresentation> Credentials,
    [property: JsonPropertyName("attributes")] Dictionary<string, IReadOnlyList<string>> Attributes);
