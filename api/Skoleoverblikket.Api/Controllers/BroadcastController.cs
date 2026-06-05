using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Email;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/broadcast-email")]
[Authorize]
public sealed class BroadcastController(
	AppDbContext db,
	ITenantContext tenantContext,
	IAuthorizationService authz,
	IEmailSender emailSender,
	IOptions<SmtpOptions> smtpOptions,
	ILogger<BroadcastController> logger) : ControllerBase
{
	public record BroadcastRequest(
		Guid? ClassId,
		[Required, StringLength(200, MinimumLength = 1)] string Subject,
		[Required, StringLength(10000, MinimumLength = 1)] string Body);

	public record BroadcastPreviewDto(int RecipientCount);

	public record BroadcastLogDto(
		Guid Id,
		string SenderName,
		string? ClassName,
		string Subject,
		int RecipientCount,
		DateTimeOffset SentAt);

	[HttpPost("preview")]
	public async Task<ActionResult<BroadcastPreviewDto>> Preview([FromBody] BroadcastPreviewRequest req, CancellationToken cancellationToken)
	{
		if (req.ClassId.HasValue)
		{
			var authResult = await authz.AuthorizeAsync(User, req.ClassId.Value, Policies.EditClass);
			if (!authResult.Succeeded)
			{
				return Forbid();
			}
		}
		else if (!User.IsInRole(Roles.Admin))
		{
			return Forbid();
		}

		var count = await ResolveRecipientCount(req.ClassId, cancellationToken);
		return Ok(new BroadcastPreviewDto(count));
	}

	public record BroadcastPreviewRequest(Guid? ClassId);

	[HttpPost]
	public async Task<ActionResult> Send([FromBody] BroadcastRequest req, CancellationToken cancellationToken)
	{
		if (User.IsInRole(Roles.Parent))
		{
			return Forbid();
		}

		if (req.ClassId.HasValue)
		{
			var authResult = await authz.AuthorizeAsync(User, req.ClassId.Value, Policies.EditClass);
			if (!authResult.Succeeded)
			{
				return Forbid();
			}
		}
		// No ClassId set, Admin required
		else if (!User.IsInRole(Roles.Admin))
		{
			return Forbid();
		}

		var callerSubject = User.GetKeycloakSubject();

		var caller = await db.Staff
			.AsNoTracking()
			.FirstOrDefaultAsync(s => s.KeycloakSubject == callerSubject, cancellationToken);

		if (caller is null)
		{
			return Unauthorized();
		}

		var school = await db.Schools.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
		var schoolName = school?.Name ?? "Skoleoverblikket";

		var recipients = await ResolveRecipients(req.ClassId, cancellationToken);

		var footer = $"<p style=\"font-size:12px;color:#888;\">Du modtager denne e-mail fra {HtmlEncoder.Default.Encode(schoolName)}. " +
					 "Log ind og gå til Indstillinger for at ændre dine e-mailpræferencer.</p>";

		var bccAddresses = recipients
			.Where(r => !string.IsNullOrWhiteSpace(r.Email))
			.Select(r => r.Email)
			.ToList();

		int sent = 0;
		if (bccAddresses.Count > 0)
		{
			try
			{
				var html = BuildHtml(req.Body, footer);
				await emailSender.SendAsync(new EmailMessage(smtpOptions.Value.FromAddress, req.Subject, html, Bcc: bccAddresses), cancellationToken);
				sent = bccAddresses.Count;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to send broadcast email");
			}
		}

		db.BroadcastEmails.Add(new BroadcastEmail
		{
			Id = Guid.NewGuid(),
			TenantId = tenantContext.TenantId,
			SenderStaffId = caller.Id,
			SenderName = caller.Name,
			ClassId = req.ClassId,
			Subject = req.Subject,
			Body = req.Body,
			RecipientCount = sent,
		});
		await db.SaveChangesAsync(cancellationToken);

		return Ok(new { recipientCount = sent });
	}

	[HttpGet("log")]
	[Authorize(Roles = Roles.Admin)]
	public async Task<ActionResult<IReadOnlyList<BroadcastLogDto>>> GetLog(CancellationToken cancellationToken)
	{
		var logs = await db.BroadcastEmails
			.AsNoTracking()
			.OrderByDescending(b => b.SentAt)
			.Take(100)
			.ToListAsync(cancellationToken);

		var classIds = logs.Where(b => b.ClassId.HasValue).Select(b => b.ClassId!.Value).Distinct().ToList();
		var classes = await db.Classes
			.AsNoTracking()
			.Where(c => classIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

		var dtos = logs.Select(b => new BroadcastLogDto(
			b.Id,
			b.SenderName,
			b.ClassId.HasValue ? classes.GetValueOrDefault(b.ClassId.Value) : null,
			b.Subject,
			b.RecipientCount,
			b.SentAt)).ToList();

		return Ok(dtos);
	}

	private async Task<int> ResolveRecipientCount(Guid? classId, CancellationToken cancellationToken)
	{
		var recipients = await ResolveRecipients(classId, cancellationToken);
		return recipients.Count;
	}

	private async Task<List<(string Name, string Email)>> ResolveRecipients(Guid? classId, CancellationToken cancellationToken)
	{
		IQueryable<Parent> query;

		if (classId.HasValue)
		{
			var studentIds = db.Students.Where(s => s.ClassId == classId.Value).Select(s => s.Id);
			query = db.Parents.Where(p => p.Students.Any(s => studentIds.Contains(s.Id)));
		}
		else
		{
			query = db.Parents;
		}

		var parents = await query
			.AsNoTracking()
			.Where(p => !string.IsNullOrEmpty(p.Email))
			.Select(p => new { p.Name, p.Email })
			.ToListAsync(cancellationToken);

		return parents.Select(p => (p.Name, p.Email!)).ToList();
	}

	private static string BuildHtml(string body, string footer)
	{
		var sb = new StringBuilder();
		sb.Append("<div style=\"font-family:sans-serif;max-width:600px;\">");
		foreach (var line in body.Split('\n'))
		{
			sb.Append("<p>");
			sb.Append(HtmlEncoder.Default.Encode(line));
			sb.Append("</p>");
		}

		sb.Append(footer);
		sb.Append("</div>");
		return sb.ToString();
	}
}
