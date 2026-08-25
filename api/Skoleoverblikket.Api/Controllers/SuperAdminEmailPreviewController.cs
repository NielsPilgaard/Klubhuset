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

		var html = StaffInvitationEmail.BuildHtml(name, school, link, withPassword ? password : null);
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

		var html = ParentInvitationEmail.BuildHtml(name, school, link, withPassword ? password : null);
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
