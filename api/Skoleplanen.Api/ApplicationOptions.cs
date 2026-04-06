using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api;

public sealed class ApplicationOptions
{
    public const string SectionName = "App";

    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; init; } = "http://localhost:5173";
}
