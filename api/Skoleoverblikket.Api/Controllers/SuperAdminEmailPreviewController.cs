using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Email;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/admin/email-preview")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class SuperAdminEmailPreviewController(IOptions<ApplicationOptions> appOptions) : ControllerBase
{
	[HttpGet("staff-invitation")]
	[Produces("text/html")]
	public ContentResult StaffInvitation(
		[FromQuery] string name = "Mette Hansen",
		[FromQuery] string school = "Testskolen",
		[FromQuery] bool withPassword = true)
	{
		var baseUrl = appOptions.Value.SanitizedBaseUrl;
		var link = $"{baseUrl}/invitation/preview-token";
		const string password = "Abc!12345xyz";

		var html = BuildStaffInvitation(name, school, link, withPassword ? password : null);
		return Content(html, "text/html");
	}

	[HttpGet("parent-invitation")]
	[Produces("text/html")]
	public ContentResult ParentInvitation(
		[FromQuery] string name = "Lars Andersen",
		[FromQuery] string school = "Testskolen",
		[FromQuery] bool withPassword = true)
	{
		var baseUrl = appOptions.Value.SanitizedBaseUrl;
		var link = $"{baseUrl}/parent-invitation/preview-token";
		const string password = "Abc!12345xyz";

		var html = BuildParentInvitation(name, school, link, withPassword ? password : null);
		return Content(html, "text/html");
	}

	[HttpGet("notification")]
	[Produces("text/html")]
	public ContentResult Notification(
		[FromQuery] string body = "Dit barns skema er blevet opdateret for uge 22.")
	{
		var baseUrl = appOptions.Value.SanitizedBaseUrl;
		var settingsUrl = $"{baseUrl}/indstillinger/notifikationer";

		var html = BuildNotification(body, settingsUrl);
		return Content(html, "text/html");
	}

	private static string BuildStaffInvitation(string name, string schoolName, string link, string? temporaryPassword)
	{
		var encodedName = HtmlEncoder.Default.Encode(name);
		var encodedSchoolName = HtmlEncoder.Default.Encode(schoolName);
		var encodedLink = HtmlEncoder.Default.Encode(link);

		var passwordBlock = temporaryPassword is not null
			? $"""
              <div style="margin:0 0 24px;padding:16px;background:#f3f4f6;border-radius:8px;border:1px solid #e5e7eb;">
                <p style="margin:0 0 8px;font-size:13px;color:#6b7280;">Din midlertidige adgangskode (du skal ændre den ved første login):</p>
                <span style="font-family:monospace;font-size:18px;font-weight:700;letter-spacing:0.05em;color:#111827;user-select:all;">{HtmlEncoder.Default.Encode(temporaryPassword)}</span>
              </div>
              """
			: string.Empty;

		return EmailTemplate.Wrap($"Invitation til {encodedSchoolName}", $"""
            <h1>Du er inviteret til {encodedSchoolName}</h1>
            <p>Hej {encodedName},<br><br>
            Du er inviteret til at oprette din konto på Skoleoverblikket som medarbejder på <strong>{encodedSchoolName}</strong>.
            Klik på knappen herunder for at logge ind. Linket er gyldigt i 14 dage.</p>
            {passwordBlock}
            <div class="btn-wrapper">
              <a href="{encodedLink}" class="btn">Opret konto og acceptér</a>
            </div>
            <div class="notice">
              <p style="margin-bottom:0;">Har du problemer med knappen? Kopier dette link direkte i din browser:<br>
              <a href="{encodedLink}" style="color:#1f6321;word-break:break-all;">{encodedLink}</a></p>
            </div>
            """);
	}

	private static string BuildParentInvitation(string name, string schoolName, string link, string? temporaryPassword)
	{
		var encodedName = HtmlEncoder.Default.Encode(name);
		var encodedSchoolName = HtmlEncoder.Default.Encode(schoolName);
		var encodedLink = HtmlEncoder.Default.Encode(link);

		var passwordBlock = temporaryPassword is not null
			? $"""
              <div style="margin:0 0 24px;padding:16px;background:#f3f4f6;border-radius:8px;border:1px solid #e5e7eb;">
                <p style="margin:0 0 8px;font-size:13px;color:#6b7280;">Din midlertidige adgangskode (skal ændres ved første login):</p>
                <span style="font-family:monospace;font-size:18px;font-weight:700;letter-spacing:0.05em;color:#111827;">{HtmlEncoder.Default.Encode(temporaryPassword)}</span>
              </div>
              """
			: string.Empty;

		return EmailTemplate.Wrap($"Adgang til {encodedSchoolName}", $"""
            <h1>Adgang til {encodedSchoolName}</h1>
            <p>Hej {encodedName},<br><br>
            Du er inviteret til at se dit barns skema på <strong>{encodedSchoolName}</strong> via Skoleoverblikket.
            Klik på knappen herunder for at oprette din konto. Linket er gyldigt i 14 dage.</p>
            {passwordBlock}
            <div class="btn-wrapper">
              <a href="{encodedLink}" class="btn">Opret konto</a>
            </div>
            <div class="notice">
              <p style="margin-bottom:0;">Har du problemer med knappen? Kopier dette link direkte i din browser:<br>
              <a href="{encodedLink}" style="color:#1f6321;word-break:break-all;">{encodedLink}</a></p>
            </div>
            """);
	}

	private static string BuildNotification(string body, string settingsUrl)
	{
		var encodedBody = HtmlEncoder.Default.Encode(body);
		var encodedSettingsUrl = HtmlEncoder.Default.Encode(settingsUrl);

		return EmailTemplate.Wrap("Notifikation", $"""
            <p>{encodedBody}</p>
            <div class="notice">
              <p style="margin-bottom:0;">Du modtager denne e-mail, fordi du har slået e-mailnotifikationer til.
              <a href="{encodedSettingsUrl}" style="color:#1f6321;">Administrér notifikationer</a></p>
            </div>
            """);
	}
}
