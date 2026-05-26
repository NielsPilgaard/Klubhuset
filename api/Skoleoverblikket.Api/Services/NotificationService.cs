using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Email;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Services;

public sealed class NotificationService(AppDbContext db, ITenantContext tenantContext, IEmailSender email, IOptions<ApplicationOptions> appOptions) : INotificationService
{
	public async Task CreateAsync(
		Guid recipientId,
		RecipientType recipientType,
		NotificationType type,
		Guid? referenceId,
		string body,
		CancellationToken ct)
	{
		// 1. Check preference — find by (UserId==recipientId && UserType==recipientType && Type==type)
		//    If not found, default InApp=true, Email=true
		var pref = await db.NotificationPreferences
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.UserId == recipientId && p.UserType == recipientType && p.Type == type, ct);
		bool inApp = pref?.InApp ?? true;
		bool emailEnabled = pref?.Email ?? true;

		// 2. If inApp: insert Notification row
		if (inApp)
		{
			db.Notifications.Add(new Notification
			{
				Id = Guid.NewGuid(),
				TenantId = tenantContext.TenantId,
				RecipientId = recipientId,
				RecipientType = recipientType,
				Type = type,
				ReferenceId = referenceId,
				Body = body,
				CreatedAt = DateTimeOffset.UtcNow,
			});
			await db.SaveChangesAsync(ct);
		}

		// 3. If email: get recipient email
		if (emailEnabled)
		{
			string? recipientEmail = recipientType == RecipientType.Parent
				? await db.Parents.AsNoTracking().Where(p => p.Id == recipientId).Select(p => p.Email).FirstOrDefaultAsync(ct)
				: await db.Staff.AsNoTracking().Where(s => s.Id == recipientId).Select(s => s.Email).FirstOrDefaultAsync(ct);

			if (!string.IsNullOrEmpty(recipientEmail))
			{
				var baseUrl = appOptions.Value.SanitizedBaseUrl;
				var settingsUrl = $"{baseUrl}/indstillinger/notifikationer";
				await email.SendAsync(new EmailMessage(
					To: recipientEmail,
					Subject: body,
					HtmlBody: BuildHtmlEmail(body, settingsUrl),
					PlainTextBody: BuildPlainEmail(body, settingsUrl)
				), ct);
			}
		}
	}

	private static string BuildHtmlEmail(string body, string settingsUrl)
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

	private static string BuildPlainEmail(string body, string settingsUrl) =>
		$"{body}\n\n---\nDu modtager denne e-mail, fordi du har slået e-mailnotifikationer til.\nAdministrér notifikationer: {settingsUrl}\n";
}
