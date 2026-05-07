using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Email;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Services;

public sealed class StaffInvitationService(
	AppDbContext db,
	ITenantContext tenant,
	IEmailSender email,
	IConfiguration config,
	KeycloakAdminService keycloakAdmin,
	ILogger<StaffInvitationService> logger)
{
	private static readonly TimeSpan InvitationValidity = TimeSpan.FromDays(14);

	public async Task<StaffInvitation> CreateAndSendAsync(Staff staff, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(staff.Email))
		{
			throw new InvalidOperationException("Staff member has no email address.");
		}

		// Expire any existing pending invitations for this staff member
		var existing = await db.StaffInvitations
							   .Where(i => i.StaffId == staff.Id && i.AcceptedAt == null)
							   .ToListAsync(ct);

		db.StaffInvitations.RemoveRange(existing);

		var token = GenerateToken();
		var invitation = new StaffInvitation
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			StaffId = staff.Id,
			Email = staff.Email,
			Token = token,
			ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationValidity),
		};

		db.StaffInvitations.Add(invitation);
		await db.SaveChangesAsync(ct);

		// Create a Keycloak account for the invited user if one doesn't exist yet.
		// The account has no password; UPDATE_PASSWORD is set as a required action so
		// Keycloak prompts the user to choose a password on first login.
		if (string.IsNullOrWhiteSpace(staff.KeycloakSubject))
		{
			try
			{
				var nameParts = staff.Name.Split(' ', 2);
				var firstName = nameParts[0];
				var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
				var keycloakSubject = await keycloakAdmin.CreateStaffUserAsync(staff.Email, firstName, lastName, ct);
				staff.KeycloakSubject = keycloakSubject;
				await db.SaveChangesAsync(ct);
			}
			catch (KeycloakException ex)
			{
				logger.LogWarning(ex, "Could not pre-create Keycloak account for invited staff {Email}; invitation email will still be sent", staff.Email);
			}
		}

		var school = await db.Schools
							 .IgnoreQueryFilters()
							 .Where(s => s.Id == tenant.TenantId)
							 .Select(s => s.Name)
							 .FirstOrDefaultAsync(ct) ??
					 "Skoleoverblikket";

		var baseUrl = config["App:BaseUrl"];
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			throw new InvalidOperationException(
				"Configuration 'App:BaseUrl' is not set or is empty. This must be configured to generate valid invitation links.");
		}

		baseUrl = baseUrl.TrimEnd('/');
		var link = $"{baseUrl}/invitation/{token}";

		await email.SendAsync(new EmailMessage(
								  To: staff.Email,
								  Subject: $"Invitation til {school} på Skoleoverblikket",
								  HtmlBody: BuildHtmlEmail(staff.Name, school, link),
								  PlainTextBody: BuildPlainEmail(staff.Name, school, link)
							  ),
							  ct);

		return invitation;
	}

	public async Task<StaffInvitation?> FindValidAsync(string token, CancellationToken ct) =>
		await db.StaffInvitations
				.IgnoreQueryFilters()
				.Include(i => i.Staff)
				.FirstOrDefaultAsync(
					i => i.Token == token && i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow,
					ct);

	public async Task MarkAcceptedAsync(StaffInvitation invitation, string keycloakSubject, CancellationToken ct)
	{
		try
		{
			invitation.AcceptedAt = DateTimeOffset.UtcNow;
			var staff = await db.Staff
								.IgnoreQueryFilters()
								.FirstAsync(s => s.Id == invitation.StaffId, ct);

			staff.KeycloakSubject = keycloakSubject;
			await db.SaveChangesAsync(ct);
		}
		catch (DbUpdateConcurrencyException)
		{
			// Another request has already accepted this invitation
			// Refresh the entity and check if it's already accepted
			await db.Entry(invitation).ReloadAsync(ct);
			if (invitation.AcceptedAt != null)
			{
				// Invitation was already accepted, which is fine
				return;
			}

			// If AcceptedAt is still null after reload, this is an unexpected error
			throw new InvalidOperationException(
				"Failed to accept invitation due to concurrent modification. Please try again.");
		}
	}

	private static string GenerateToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		return Convert.ToBase64String(bytes)
					  .Replace('+', '-')
					  .Replace('/', '_')
					  .TrimEnd('=');
	}

	private static string BuildHtmlEmail(string name, string schoolName, string link)
	{
		var encodedName = HtmlEncoder.Default.Encode(name);
		var encodedSchoolName = HtmlEncoder.Default.Encode(schoolName);
		var encodedLink = HtmlEncoder.Default.Encode(link);

		return $"""
				<!DOCTYPE html>
				<html lang="da">
				<head><meta charset="utf-8" /><title>Invitation til {encodedSchoolName}</title></head>
				<body style="font-family:system-ui,sans-serif;color:#111;background:#f9fafb;margin:0;padding:32px;">
				  <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;padding:40px;border:1px solid #e5e7eb;">
				    <div style="margin-bottom:28px;">
				      <img src="https://skoleoverblikket.dk/logo.png" alt="Skoleoverblikket" height="40" style="display:block;height:40px;width:auto;" />
				    </div>
				    <h1 style="font-size:20px;font-weight:600;color:#111827;margin:0 0 16px;">Du er inviteret til {encodedSchoolName}</h1>
				    <p style="color:#374151;margin:0 0 24px;">Hej {encodedName},<br><br>
				    Du er inviteret til at oprette din konto på Skoleoverblikket som medarbejder på <strong>{encodedSchoolName}</strong>.
				    Klik på knappen herunder for at oprette din konto. Linket er gyldigt i 14 dage.</p>
				    <a href="{encodedLink}" style="display:inline-block;padding:12px 24px;background:#1f6321;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;">
				      Opret konto
				    </a>
				    <p style="margin:24px 0 0;font-size:13px;color:#9ca3af;">
				      Eller kopiér dette link: <a href="{encodedLink}" style="color:#1d4ed8;">{encodedLink}</a>
				    </p>
				  </div>
				</body>
				</html>
				""";
	}

	private static string BuildPlainEmail(string name, string schoolName, string link) =>
		$"Hej {name},\n\nDu er inviteret til {schoolName} på Skoleoverblikket.\n\nOpret din konto her:\n{link}\n\nLinket er gyldigt i 14 dage.\n";
}
