using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Tenancy;
using System.Security.Claims;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/messages")]
[Authorize]
public sealed class MessagesController(
	AppDbContext db,
	ITenantContext tenantContext,
	INotificationService notificationService) : ControllerBase
{
	public record InboxMessageDto(
		Guid Id,
		Guid SenderId,
		RecipientType SenderType,
		string SenderName,
		string Subject,
		string Body,
		DateTimeOffset SentAt,
		DateTimeOffset? ReadAt);

	public record SentMessageDto(
		Guid Id,
		Guid RecipientId,
		RecipientType RecipientType,
		string RecipientName,
		string Subject,
		string Body,
		DateTimeOffset SentAt,
		DateTimeOffset? ReadAt);

	public record SendMessageRequest(
		Guid RecipientId,
		RecipientType RecipientType,
		string Subject,
		string Body);

	public record RecipientDto(
		Guid Id,
		string Name,
		RecipientType Type,
		string? AvatarUrl);

	private async Task<(Guid Id, string Name, RecipientType Type)?> ResolveCallerAsync(CancellationToken ct)
	{
		var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

		if (User.IsInRole(Roles.Parent))
		{
			var parent = await db.Parents
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.KeycloakSubject == sub, ct);

			if (parent is null)
			{
				return null;
			}

			return (parent.Id, parent.Name, RecipientType.Parent);
		}
		else
		{
			var staff = await db.Staff
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.KeycloakSubject == sub, ct);

			if (staff is null)
			{
				return null;
			}

			return (staff.Id, staff.Name, RecipientType.Staff);
		}
	}

	[HttpGet("inbox")]
	public async Task<ActionResult<IReadOnlyList<InboxMessageDto>>> GetInbox(CancellationToken ct)
	{
		var caller = await ResolveCallerAsync(ct);
		if (caller is null)
		{
			return Forbid();
		}

		var (callerId, _, _) = caller.Value;

		var messages = await db.Messages
			.AsNoTracking()
			.Where(m => m.RecipientId == callerId)
			.OrderByDescending(m => m.SentAt)
			.Take(50)
			.ToListAsync(ct);

		var parentIds = messages.Where(m => m.SenderType == RecipientType.Parent).Select(m => m.SenderId).Distinct().ToList();
		var staffIds = messages.Where(m => m.SenderType == RecipientType.Staff).Select(m => m.SenderId).Distinct().ToList();

		var parents = await db.Parents.AsNoTracking()
			.Where(p => parentIds.Contains(p.Id))
			.Select(p => new { p.Id, p.Name })
			.ToListAsync(ct);

		var staffMembers = await db.Staff.AsNoTracking()
			.Where(s => staffIds.Contains(s.Id))
			.Select(s => new { s.Id, s.Name })
			.ToListAsync(ct);

		var parentMap = parents.ToDictionary(p => p.Id, p => p.Name);
		var staffMap = staffMembers.ToDictionary(s => s.Id, s => s.Name);

		var dtos = messages.Select(m =>
		{
			var senderName = m.SenderType == RecipientType.Parent
				? parentMap.GetValueOrDefault(m.SenderId, "Forælder")
				: staffMap.GetValueOrDefault(m.SenderId, "Medarbejder");
			return new InboxMessageDto(m.Id, m.SenderId, m.SenderType, senderName, m.Subject, m.Body, m.SentAt, m.ReadAt);
		}).ToList();

		return Ok(dtos);
	}

	[HttpGet("sent")]
	public async Task<ActionResult<IReadOnlyList<SentMessageDto>>> GetSent(CancellationToken ct)
	{
		var caller = await ResolveCallerAsync(ct);
		if (caller is null)
		{
			return Forbid();
		}

		var (callerId, _, _) = caller.Value;

		var messages = await db.Messages
			.AsNoTracking()
			.Where(m => m.SenderId == callerId)
			.OrderByDescending(m => m.SentAt)
			.Take(50)
			.ToListAsync(ct);

		var parentIds = messages.Where(m => m.RecipientType == RecipientType.Parent).Select(m => m.RecipientId).Distinct().ToList();
		var staffIds = messages.Where(m => m.RecipientType == RecipientType.Staff).Select(m => m.RecipientId).Distinct().ToList();

		var parents = await db.Parents.AsNoTracking()
			.Where(p => parentIds.Contains(p.Id))
			.Select(p => new { p.Id, p.Name })
			.ToListAsync(ct);

		var staffMembers = await db.Staff.AsNoTracking()
			.Where(s => staffIds.Contains(s.Id))
			.Select(s => new { s.Id, s.Name })
			.ToListAsync(ct);

		var parentMap = parents.ToDictionary(p => p.Id, p => p.Name);
		var staffMap = staffMembers.ToDictionary(s => s.Id, s => s.Name);

		var dtos = messages.Select(m =>
		{
			var recipientName = m.RecipientType == RecipientType.Parent
				? parentMap.GetValueOrDefault(m.RecipientId, "Forælder")
				: staffMap.GetValueOrDefault(m.RecipientId, "Medarbejder");
			return new SentMessageDto(m.Id, m.RecipientId, m.RecipientType, recipientName, m.Subject, m.Body, m.SentAt, m.ReadAt);
		}).ToList();

		return Ok(dtos);
	}

	[HttpPost]
	public async Task<IActionResult> SendMessage(
		[FromBody] SendMessageRequest req,
		CancellationToken ct)
	{
		var caller = await ResolveCallerAsync(ct);
		if (caller is null)
		{
			return Forbid();
		}

		var (callerId, callerName, callerType) = caller.Value;

		if (callerType == RecipientType.Parent && req.RecipientType == RecipientType.Parent)
		{
			var recipientConsents = await db.Parents
				.AnyAsync(p => p.Id == req.RecipientId && p.ShareContactInfo, ct);

			if (!recipientConsents)
			{
				return Forbid();
			}
		}

		var message = new Message
		{
			Id = Guid.NewGuid(),
			TenantId = tenantContext.TenantId,
			SenderId = callerId,
			SenderType = callerType,
			RecipientId = req.RecipientId,
			RecipientType = req.RecipientType,
			Subject = req.Subject,
			Body = req.Body,
			SentAt = DateTimeOffset.UtcNow,
		};

		db.Messages.Add(message);
		await db.SaveChangesAsync(ct);

		await notificationService.CreateAsync(
			req.RecipientId,
			req.RecipientType,
			NotificationType.NewMessage,
			message.Id,
			$"{callerName} har sendt dig en besked: {req.Subject}",
			ct);

		return Created(string.Empty, new { message.Id });
	}

	[HttpPost("{id:guid}/read")]
	public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
	{
		var caller = await ResolveCallerAsync(ct);
		if (caller is null)
		{
			return Forbid();
		}

		var (callerId, _, _) = caller.Value;

		var message = await db.Messages
			.FirstOrDefaultAsync(m => m.Id == id && m.RecipientId == callerId, ct);

		if (message is null)
		{
			return NotFound();
		}

		if (message.ReadAt is null)
		{
			message.ReadAt = DateTimeOffset.UtcNow;
			await db.SaveChangesAsync(ct);
		}

		return NoContent();
	}

	[HttpGet("recipients")]
	public async Task<ActionResult<IReadOnlyList<RecipientDto>>> GetRecipients(
		[FromQuery] string q = "",
		CancellationToken ct = default)
	{
		if (q.Length < 2)
		{
			return Ok(Array.Empty<RecipientDto>());
		}

		var caller = await ResolveCallerAsync(ct);
		if (caller is null)
		{
			return Forbid();
		}

		var (callerId, _, callerType) = caller.Value;

		var results = new List<RecipientDto>();

		if (callerType == RecipientType.Parent)
		{
			var matchingStaff = await db.Staff.AsNoTracking()
				.ToListAsync(ct);

			results.AddRange(matchingStaff
				.Where(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
				.Take(10)
				.Select(s => new RecipientDto(s.Id, s.Name, RecipientType.Staff, s.AvatarUrl)));

			var matchingParents = await db.Parents.AsNoTracking()
				.Where(p => p.ShareContactInfo && p.Id != callerId)
				.ToListAsync(ct);

			results.AddRange(matchingParents
				.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
				.Take(10)
				.Select(p => new RecipientDto(p.Id, p.Name, RecipientType.Parent, p.AvatarUrl)));
		}
		else
		{
			var matchingParents = await db.Parents.AsNoTracking()
				.ToListAsync(ct);

			results.AddRange(matchingParents
				.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
				.Take(10)
				.Select(p => new RecipientDto(p.Id, p.Name, RecipientType.Parent, p.AvatarUrl)));

			var matchingStaff = await db.Staff.AsNoTracking()
				.ToListAsync(ct);

			results.AddRange(matchingStaff
				.Where(s => s.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
				.Take(10)
				.Select(s => new RecipientDto(s.Id, s.Name, RecipientType.Staff, s.AvatarUrl)));
		}

		return Ok(results.Take(20).ToList());
	}
}
