using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    
    public string Host { get; init; } = "smtp.tem.scaleway.com";
    public int Port { get; init; } = 587;

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    
    [Required(AllowEmptyStrings = false)]
    public string FromAddress { get; init; } = "kontakt@skoleoverblikket.dk";

    public string FromName { get; init; } = "Skoleoverblikket";
}
