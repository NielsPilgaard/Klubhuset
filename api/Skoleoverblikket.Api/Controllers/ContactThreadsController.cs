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
[Route("api/v1/contact-threads")]
[Authorize]
public sealed class ContactThreadsController(
	AppDbContext db,
	ITenantContext tenantContext,
	INotificationService notificationService) : ControllerBase
{
	public record ContactThreadDto(
		Guid Id,
		Guid StudentId,
		string StudentName,
		string? LastMessageBody,
		DateTimeOffset? LastMessageSentAt,
		SenderType? LastMessageSenderType,
		int UnreadCount);

	public record ContactMessageDto(
		Guid Id,
		SenderType SenderType,
		Guid SenderId,
		string SenderName,
		string Body,
		DateTimeOffset SentAt,
		DateTimeOffset? ReadAt);

	public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

	public record FindOrCreateThreadRequest(Guid StudentId, string Body);

	public record AddMessageRequest(string Body);

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<ContactThreadDto>>> GetThreads(CancellationToken ct)
	{
		var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var isParent = User.IsInRole(Roles.Parent);

		IQueryable<ContactThread> query = db.ContactThreads
			.AsNoTracking()
			.Include(t => t.Student)
			.Include(t => t.Messages);

		if (isParent)
		{
			var parent = await db.Parents
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.KeycloakSubject == sub, ct);

			if (parent is null)
			{
				return Ok(Array.Empty<ContactThreadDto>());
			}

			query = query.Where(t => t.Student != null && t.Student.Parents.Any(p => p.Id == parent.Id));
		}

		var callerSenderType = isParent ? SenderType.Parent : SenderType.Staff;

		var threads = await query.ToListAsync(ct);

		var result = threads.Select(t =>
		{
			var lastMsg = t.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
			var unread = t.Messages.Count(m => m.ReadAt == null && m.SenderType != callerSenderType);
			return new ContactThreadDto(
				t.Id,
				t.StudentId,
				t.Student?.Name ?? string.Empty,
				lastMsg?.Body,
				lastMsg?.SentAt,
				lastMsg?.SenderType,
				unread);
		}).ToList();

		return Ok(result);
	}

	[HttpGet("{threadId:guid}/messages")]
	public async Task<ActionResult<PagedResult<ContactMessageDto>>> GetMessages(
		Guid threadId,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		CancellationToken ct = default)
	{
		if (pageSize > 50)
		{
			pageSize = 50;
		}

		var thread = await db.ContactThreads
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == threadId, ct);

		if (thread is null)
		{
			return NotFound();
		}

		var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var isParent = User.IsInRole(Roles.Parent);

		if (isParent)
		{
			var hasAccess = await db.Parents
				.AnyAsync(p => p.KeycloakSubject == sub && p.Students.Any(s => s.Id == thread.StudentId), ct);
			if (!hasAccess)
			{
				return Forbid();
			}
		}

		var total = await db.ContactMessages.CountAsync(m => m.ThreadId == threadId, ct);

		var messages = await db.ContactMessages
			.AsNoTracking()
			.Where(m => m.ThreadId == threadId)
			.OrderBy(m => m.SentAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		var parentIds = messages.Where(m => m.SenderType == SenderType.Parent).Select(m => m.SenderId).Distinct().ToList();
		var staffIds = messages.Where(m => m.SenderType == SenderType.Staff).Select(m => m.SenderId).Distinct().ToList();

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
			var senderName = m.SenderType == SenderType.Parent
				? parentMap.GetValueOrDefault(m.SenderId, "Forælder")
				: staffMap.GetValueOrDefault(m.SenderId, "Medarbejder");
			return new ContactMessageDto(m.Id, m.SenderType, m.SenderId, senderName, m.Body, m.SentAt, m.ReadAt);
		}).ToList();

		return Ok(new PagedResult<ContactMessageDto>(dtos, total, page, pageSize));
	}

	[HttpPost]
	public async Task<IActionResult> FindOrCreateThread(
		[FromBody] FindOrCreateThreadRequest req,
		CancellationToken ct)
	{
		var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var isParent = User.IsInRole(Roles.Parent);

		Guid senderId;
		string senderName;
		SenderType senderType;

		if (isParent)
		{
			var parent = await db.Parents
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.KeycloakSubject == sub, ct);

			if (parent is null)
			{
				return Forbid();
			}

			var parentOwnsStudent = await db.Parents
				.AnyAsync(p => p.KeycloakSubject == sub && p.Students.Any(s => s.Id == req.StudentId), ct);

			if (!parentOwnsStudent)
			{
				return Forbid();
			}

			senderId = parent.Id;
			senderName = parent.Name;
			senderType = SenderType.Parent;
		}
		else
		{
			var staff = await db.Staff
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.KeycloakSubject == sub, ct);

			if (staff is null)
			{
				return Forbid();
			}

			senderId = staff.Id;
			senderName = staff.Name;
			senderType = SenderType.Staff;
		}

		var studentName = await db.Students
			.AsNoTracking()
			.Where(s => s.Id == req.StudentId)
			.Select(s => s.Name)
			.FirstOrDefaultAsync(ct) ?? string.Empty;

		var thread = await db.ContactThreads
			.FirstOrDefaultAsync(t => t.StudentId == req.StudentId, ct);

		if (thread is null)
		{
			thread = new ContactThread
			{
				Id = Guid.NewGuid(),
				TenantId = tenantContext.TenantId,
				StudentId = req.StudentId,
				CreatedAt = DateTimeOffset.UtcNow,
			};
			db.ContactThreads.Add(thread);
		}

		var message = new ContactMessage
		{
			Id = Guid.NewGuid(),
			TenantId = tenantContext.TenantId,
			ThreadId = thread.Id,
			SenderType = senderType,
			SenderId = senderId,
			Body = req.Body,
			SentAt = DateTimeOffset.UtcNow,
		};
		db.ContactMessages.Add(message);
		await db.SaveChangesAsync(ct);

		await SendNotificationsAsync(thread.Id, req.StudentId, studentName, senderName, senderType, ct);

		return CreatedAtAction(nameof(GetMessages), new { threadId = thread.Id }, new { ThreadId = thread.Id });
	}

	[HttpPost("{threadId:guid}/messages")]
	public async Task<IActionResult> AddMessage(
		Guid threadId,
		[FromBody] AddMessageRequest req,
		CancellationToken ct)
	{
		var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var isParent = User.IsInRole(Roles.Parent);

		var thread = await db.ContactThreads
			.FirstOrDefaultAsync(t => t.Id == threadId, ct);

		if (thread is null)
		{
			return NotFound();
		}

		Guid senderId;
		string senderName;
		SenderType senderType;

		if (isParent)
		{
			var parent = await db.Parents
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.KeycloakSubject == sub, ct);

			if (parent is null)
			{
				return Forbid();
			}

			var hasAccess = await db.Parents
				.AnyAsync(p => p.KeycloakSubject == sub && p.Students.Any(s => s.Id == thread.StudentId), ct);

			if (!hasAccess)
			{
				return Forbid();
			}

			senderId = parent.Id;
			senderName = parent.Name;
			senderType = SenderType.Parent;
		}
		else
		{
			var staff = await db.Staff
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.KeycloakSubject == sub, ct);

			if (staff is null)
			{
				return Forbid();
			}

			senderId = staff.Id;
			senderName = staff.Name;
			senderType = SenderType.Staff;
		}

		var studentName = await db.Students
			.AsNoTracking()
			.Where(s => s.Id == thread.StudentId)
			.Select(s => s.Name)
			.FirstOrDefaultAsync(ct) ?? string.Empty;

		var message = new ContactMessage
		{
			Id = Guid.NewGuid(),
			TenantId = tenantContext.TenantId,
			ThreadId = threadId,
			SenderType = senderType,
			SenderId = senderId,
			Body = req.Body,
			SentAt = DateTimeOffset.UtcNow,
		};
		db.ContactMessages.Add(message);
		await db.SaveChangesAsync(ct);

		await SendNotificationsAsync(threadId, thread.StudentId, studentName, senderName, senderType, ct);

		return Created(string.Empty, null);
	}

	[HttpPost("{threadId:guid}/read")]
	public async Task<IActionResult> MarkRead(Guid threadId, CancellationToken ct)
	{
		var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		var isParent = User.IsInRole(Roles.Parent);

		var thread = await db.ContactThreads
			.FirstOrDefaultAsync(t => t.Id == threadId, ct);

		if (thread is null)
		{
			return NotFound();
		}

		if (isParent)
		{
			var hasAccess = await db.Parents
				.AnyAsync(p => p.KeycloakSubject == sub && p.Students.Any(s => s.Id == thread.StudentId), ct);

			if (!hasAccess)
			{
				return Forbid();
			}
		}

		var callerSenderType = isParent ? SenderType.Parent : SenderType.Staff;
		var now = DateTimeOffset.UtcNow;

		var unread = await db.ContactMessages
			.Where(m => m.ThreadId == threadId && m.ReadAt == null && m.SenderType != callerSenderType)
			.ToListAsync(ct);

		foreach (var msg in unread)
		{
			msg.ReadAt = now;
		}

		await db.SaveChangesAsync(ct);

		return NoContent();
	}

	private async Task SendNotificationsAsync(
		Guid threadId,
		Guid studentId,
		string studentName,
		string senderName,
		SenderType senderType,
		CancellationToken ct)
	{
		if (senderType == SenderType.Parent)
		{
			var allStaff = await db.Staff.AsNoTracking().ToListAsync(ct);
			foreach (var s in allStaff)
			{
				await notificationService.CreateAsync(
					s.Id,
					RecipientType.Staff,
					NotificationType.NewContactMessage,
					threadId,
					$"{senderName} har sendt en besked om {studentName}",
					ct);
			}
		}
		else
		{
			var studentParents = await db.Students
				.AsNoTracking()
				.Where(s => s.Id == studentId)
				.SelectMany(s => s.Parents)
				.ToListAsync(ct);

			foreach (var p in studentParents)
			{
				await notificationService.CreateAsync(
					p.Id,
					RecipientType.Parent,
					NotificationType.NewContactMessage,
					threadId,
					$"{senderName} har sendt en besked om {studentName}",
					ct);
			}
		}
	}
}
