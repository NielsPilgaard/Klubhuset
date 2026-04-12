using System.Text.Json.Serialization;
using Refit;

namespace Skoleplanen.Api.Auth;

/// <summary>Token endpoint — base URL: {authority}/protocol/openid-connect</summary>
public interface IKeycloakTokenApi
{
    [Post("/token")]
    Task<TokenResponse> GetTokenAsync([Body(BodySerializationMethod.UrlEncoded)] TokenRequest request, CancellationToken ct);
}
public record TokenRequest(
    [property: AliasAs("grant_type")] string GrantType,
    [property: AliasAs("client_id")] string ClientId,
    [property: AliasAs("client_secret")] string ClientSecret);

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken);
