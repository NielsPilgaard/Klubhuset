using System.Text.Json.Serialization;
using Refit;

namespace Skoleoverblikket.Api.Auth;

/// <summary>Token endpoint — base URL: {authority}/protocol/openid-connect</summary>
public interface IKeycloakTokenApi
{
	[Post("/token")]
	Task<TokenResponse> GetTokenAsync([Body(BodySerializationMethod.UrlEncoded)] TokenRequest request, CancellationToken ct);

	[Post("/token")]
	Task<TokenResponse> GetPasswordTokenAsync([Body(BodySerializationMethod.UrlEncoded)] PasswordTokenRequest request, CancellationToken ct);
}

public record TokenRequest(
	[property: AliasAs("grant_type")] string GrantType,
	[property: AliasAs("client_id")] string ClientId,
	[property: AliasAs("client_secret")] string ClientSecret);

public record PasswordTokenRequest(
	[property: AliasAs("grant_type")] string GrantType,
	[property: AliasAs("client_id")] string ClientId,
	[property: AliasAs("username")] string Username,
	[property: AliasAs("password")] string Password,
	[property: AliasAs("scope")] string Scope);

public record TokenResponse(
	[property: JsonPropertyName("access_token")] string AccessToken,
	[property: JsonPropertyName("refresh_token")] string? RefreshToken,
	[property: JsonPropertyName("expires_in")] int ExpiresIn,
	[property: JsonPropertyName("token_type")] string TokenType);
