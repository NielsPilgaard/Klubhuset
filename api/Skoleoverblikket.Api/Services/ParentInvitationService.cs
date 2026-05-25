using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Email;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Services;

public sealed class ParentInvitationService(
	AppDbContext db,
	ITenantContext tenant,
	IEmailSender email,
	IConfiguration config,
	KeycloakAdminService keycloakAdmin)
{
	private static readonly TimeSpan InvitationValidity = TimeSpan.FromDays(14);
	private readonly string BaseUrl = (config["App:BaseUrl"]
				?? throw new InvalidOperationException("Configuration 'App:BaseUrl' is not set.")).TrimEnd('/');

	public async Task<ParentInvitation> CreateAndSendAsync(Parent parent, CancellationToken ct)
	{
		var pending = await db.ParentInvitations
			.Where(i => i.ParentId == parent.Id && i.AcceptedAt == null)
			.ToListAsync(ct);
		db.ParentInvitations.RemoveRange(pending);

		var token = GenerateToken();
		var invitation = new ParentInvitation
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			ParentId = parent.Id,
			Email = parent.Email,
			Token = token,
			ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationValidity),
		};

		db.ParentInvitations.Add(invitation);
		await db.SaveChangesAsync(ct);

		var temporaryPassword = await EnsureKeycloakAccountAsync(parent, ct);

		var school = await db.Schools
			.IgnoreQueryFilters()
			.Where(s => s.Id == tenant.TenantId)
			.Select(s => s.Name)
			.FirstOrDefaultAsync(ct) ?? "Skoleoverblikket";

		var link = $"{BaseUrl}/parent-invitation/{token}";

		await email.SendAsync(new EmailMessage(
			To: parent.Email,
			Subject: $"Invitation til {school} pa Skoleoverblikket",
			HtmlBody: BuildHtmlEmail(parent.Name, school, link, temporaryPassword),
			PlainTextBody: BuildPlainEmail(parent.Name, school, link, temporaryPassword)
		), ct);

		return invitation;
	}

	private async Task<string?> EnsureKeycloakAccountAsync(Parent parent, CancellationToken ct)
	{
		if (!string.IsNullOrWhiteSpace(parent.KeycloakSubject))
		{

			return null;
		}


		if (string.IsNullOrWhiteSpace(parent.Name))
		{

			throw new InvalidOperationException($"Cannot create Keycloak account for parent {parent.Email}: name is empty.");
		}


		var temporaryPassword = GenerateTemporaryPassword();
		var nameParts = parent.Name.Trim().Split(' ', 2);
		parent.KeycloakSubject = await keycloakAdmin.CreateUserAsync(
			parent.Email,
			nameParts[0],
			nameParts.Length > 1 ? nameParts[1] : string.Empty,
			temporaryPassword,
			tenant.TenantId,
			realmRole: Roles.Parent,
			forcePasswordReset: true,
			ct);
		await db.SaveChangesAsync(ct);
		return temporaryPassword;
	}

	public async Task<ParentInvitation?> FindValidAsync(string token, CancellationToken ct) =>
		await db.ParentInvitations
				.IgnoreQueryFilters()
				.Include(i => i.Parent)
				.FirstOrDefaultAsync(
					i => i.Token == token && i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow,
					ct);

	public async Task MarkAcceptedAsync(ParentInvitation invitation, string keycloakSubject, CancellationToken ct)
	{
		try
		{
			invitation.AcceptedAt = DateTimeOffset.UtcNow;
			var parent = await db.Parents
								 .IgnoreQueryFilters()
								 .FirstAsync(p => p.Id == invitation.ParentId, ct);
			parent.KeycloakSubject = keycloakSubject;
			await db.SaveChangesAsync(ct);
		}
		catch (DbUpdateConcurrencyException)
		{
			await db.Entry(invitation).ReloadAsync(ct);
			if (invitation.AcceptedAt != null)
			{
				return;
			}

			throw new InvalidOperationException("Failed to accept invitation due to concurrent modification.");
		}
	}

	private static string GenerateToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
	}

	private static string GenerateTemporaryPassword()
	{
		const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
		const string lower = "abcdefghjkmnpqrstuvwxyz";
		const string digits = "23456789";
		const string special = "!@#$%&*";
		const string all = upper + lower + digits + special;

		var rng = RandomNumberGenerator.GetBytes(12);
		var chars = new char[12];
		chars[0] = upper[rng[0] % upper.Length];
		chars[1] = lower[rng[1] % lower.Length];
		chars[2] = digits[rng[2] % digits.Length];
		chars[3] = special[rng[3] % special.Length];
		for (var i = 4; i < 12; i++)
		{
			chars[i] = all[rng[i] % all.Length];
		}

		RandomNumberGenerator.Shuffle(chars.AsSpan());
		return new string(chars);
	}

	private static string BuildHtmlEmail(string name, string schoolName, string link, string? temporaryPassword)
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

		return $"""
				<!DOCTYPE html>
				<html lang="da">
				<head><meta charset="utf-8" /><title>Invitation til {encodedSchoolName}</title></head>
				<body style="font-family:system-ui,sans-serif;color:#111;background:#f9fafb;margin:0;padding:32px;">
				  <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;padding:40px;border:1px solid #e5e7eb;">
				    <h1 style="font-size:20px;font-weight:600;color:#111827;margin:0 0 16px;">Adgang til {encodedSchoolName}</h1>
				    <p style="color:#374151;margin:0 0 24px;">Hej {encodedName},<br><br>
				    Du er inviteret til at se dit barns skema på <strong>{encodedSchoolName}</strong> via Skoleoverblikket.
				    Klik pa knappen herunder for at oprette din konto. Linket er gyldigt i 14 dage.</p>
				    {passwordBlock}
				    <a href="{encodedLink}" style="display:inline-block;padding:12px 24px;background:#1f6321;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;">
				      Opret konto
				    </a>
				    <p style="margin:24px 0 0;font-size:13px;color:#9ca3af;">
				      Eller kopier dette link: <a href="{encodedLink}" style="color:#1d4ed8;">{encodedLink}</a>
				    </p>
				  </div>
				</body>
				</html>
				""";
	}

	private static string BuildPlainEmail(string name, string schoolName, string link, string? temporaryPassword)
	{
		var passwordLine = temporaryPassword is not null
			? $"\nDin midlertidige adgangskode: {temporaryPassword}\n(Skal ændres ved første login.)\n"
			: string.Empty;

		return $"""
Hej {name},

Du er inviteret til at se dit barns skema på {schoolName} via Skoleoverblikket.{passwordLine}
Opret din konto her:
{link}

Linket er gyldigt i 14 dage.
""";
	}
}
