using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Skoleoverblikket.Api.Auth;

/// <summary>
/// Fetches a client_credentials token from Keycloak and attaches it as a
/// Bearer Authorization header on every outgoing Keycloak Admin API request.
/// </summary>
public sealed class KeycloakBearerHandler(
    IKeycloakTokenApi tokenApi,
    IOptions<KeycloakOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var kc = options.Value;
        var tokenResponse = await tokenApi.GetTokenAsync(
            new TokenRequest("client_credentials", kc.AdminClientId, kc.AdminClientSecret), cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
