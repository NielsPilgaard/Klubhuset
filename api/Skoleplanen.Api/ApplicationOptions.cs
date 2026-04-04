using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api;

public sealed class ApplicationOptions
{
    public const string SectionName = "App";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}
