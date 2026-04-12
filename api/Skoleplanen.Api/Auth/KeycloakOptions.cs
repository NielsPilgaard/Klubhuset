using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api.Auth;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required(AllowEmptyStrings = false)]
    public string Authority { get; init; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = null!;

    /// <summary>
    /// Optional override for OIDC metadata discovery. Used when the API reaches
    /// Keycloak via an internal container URL that differs from the public issuer.
    /// </summary>
    public string? MetadataAddress { get; init; }

    [Required(AllowEmptyStrings = false)]
    public string AdminClientId { get; init; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string AdminClientSecret { get; init; } = null!;

    /// <summary>Base URL for the token endpoint (parent of /token).</summary>
    public string TokenBaseUrl => $"{Authority.TrimEnd('/')}/protocol/openid-connect";

    /// <summary>Derives the token endpoint from <see cref="Authority"/>.</summary>
    public string TokenEndpoint => $"{TokenBaseUrl}/token";

    /// <summary>Derives the Admin REST API base URL from <see cref="Authority"/>.</summary>
    public string AdminBaseUrl
    {
        get
        {
            var uri = new Uri(Authority.TrimEnd('/'));
            var realmSegment = uri.AbsolutePath.TrimStart('/');
            return $"{uri.Scheme}://{uri.Authority}/admin/{realmSegment}";
        }
    }
}
