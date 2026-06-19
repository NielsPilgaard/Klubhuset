using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleoverblikket.Api.Email;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/demo-request")]
[AllowAnonymous]
public sealed class DemoRequestController(IEmailSender emailSender) : ControllerBase
{
	public sealed record DemoRequestDto(
		[Required, MaxLength(200)] string Navn,
		[Required, MaxLength(200)] string Skole,
		[Required, EmailAddress, MaxLength(200)] string Email,
		[MaxLength(50)] string? Telefon,
		[MaxLength(2000)] string? Besked);

	[HttpPost]
	public async Task<IActionResult> Submit([FromBody] DemoRequestDto dto, CancellationToken cancellationToken)
	{
		var lines = new List<string>
		{
			$"Navn: {dto.Navn}",
			$"Skole: {dto.Skole}",
			$"E-mail: {dto.Email}",
		};

		if (!string.IsNullOrWhiteSpace(dto.Telefon))
		{
			lines.Add($"Telefon: {dto.Telefon}");
		}

		if (!string.IsNullOrWhiteSpace(dto.Besked))
		{
			lines.Add($"Besked: {dto.Besked}");
		}

		var plainText = string.Join("\n", lines);
		var htmlBody = "<pre>" + System.Net.WebUtility.HtmlEncode(plainText) + "</pre>";

		var safeSchoolName = dto.Skole.ReplaceLineEndings(" ").Replace("\r", " ").Replace("\n", " ");

		await emailSender.SendAsync(new EmailMessage(
			To: "kontakt@skoleoverblikket.dk",
			Subject: $"Demo-forespørgsel fra {safeSchoolName}",
			HtmlBody: htmlBody,
			PlainTextBody: plainText),
			cancellationToken);

		return Ok();
	}
}
