using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Email;
using Skoleplanen.Api.Models;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Services;

public sealed class StaffInvitationService(
    AppDbContext db,
    ITenantContext tenant,
    IEmailSender email,
    IConfiguration config)
{
    private static readonly TimeSpan InvitationValidity = TimeSpan.FromDays(14);

    public async Task<StaffInvitation> CreateAndSendAsync(Staff staff, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(staff.Email))
            throw new InvalidOperationException("Staff member has no email address.");

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

        var school = await db.Schools
            .IgnoreQueryFilters()
            .Where(s => s.Id == tenant.TenantId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct) ?? "Skoleplanen";

        var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        var link = $"{baseUrl}/invitation/{token}";

        await email.SendAsync(new EmailMessage(
            To: staff.Email,
            Subject: $"Invitation til {school} på Skoleplanen",
            HtmlBody: BuildHtmlEmail(staff.Name, school, link),
            PlainTextBody: BuildPlainEmail(staff.Name, school, link)
        ), ct);

        return invitation;
    }

    public async Task<StaffInvitation?> FindValidAsync(string token, CancellationToken ct) =>
        await db.StaffInvitations
            .IgnoreQueryFilters()
            .Include(i => i.Staff)
            .FirstOrDefaultAsync(
                i => i.Token == token
                  && i.AcceptedAt == null
                  && i.ExpiresAt > DateTimeOffset.UtcNow,
                ct);

    public async Task MarkAcceptedAsync(StaffInvitation invitation, string keycloakSubject, CancellationToken ct)
    {
        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        var staff = await db.Staff
            .IgnoreQueryFilters()
            .FirstAsync(s => s.Id == invitation.StaffId, ct);
        staff.KeycloakSubject = keycloakSubject;
        await db.SaveChangesAsync(ct);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string BuildHtmlEmail(string name, string schoolName, string link) => $"""
        <!DOCTYPE html>
        <html lang="da">
        <head><meta charset="utf-8" /><title>Invitation til {schoolName}</title></head>
        <body style="font-family:system-ui,sans-serif;color:#111;background:#f9fafb;margin:0;padding:32px;">
          <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;padding:40px;border:1px solid #e5e7eb;">
            <p style="font-size:24px;font-weight:700;color:#1d4ed8;margin:0 0 8px;">Skoleplanen</p>
            <h1 style="font-size:20px;font-weight:600;color:#111827;margin:0 0 16px;">Du er inviteret til {schoolName}</h1>
            <p style="color:#374151;margin:0 0 24px;">Hej {name},<br><br>
            Du er inviteret til at oprette din konto på Skoleplanen som medarbejder på <strong>{schoolName}</strong>.
            Klik på knappen herunder for at oprette din konto. Linket er gyldigt i 14 dage.</p>
            <a href="{link}" style="display:inline-block;padding:12px 24px;background:#1d4ed8;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;">
              Opret konto
            </a>
            <p style="margin:24px 0 0;font-size:13px;color:#9ca3af;">
              Eller kopiér dette link: <a href="{link}" style="color:#1d4ed8;">{link}</a>
            </p>
          </div>
        </body>
        </html>
        """;

    private static string BuildPlainEmail(string name, string schoolName, string link) =>
        $"Hej {name},\n\nDu er inviteret til {schoolName} på Skoleplanen.\n\nOpret din konto her:\n{link}\n\nLinket er gyldigt i 14 dage.\n";
}
