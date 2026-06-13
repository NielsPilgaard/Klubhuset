using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/board-members")]
[Authorize(Roles = Roles.Admin)]
public sealed class BoardMembersController(
	AppDbContext db,
	ITenantContext tenant,
	BoardMemberInvitationService invitationService,
	KeycloakAdminService keycloakAdmin) : ControllerBase
{
	public record BoardMemberDto(
		Guid Id,
		string Name,
		string Email,
		bool CanAccessTeacherData,
		bool HasAccount,
		DateTimeOffset CreatedAt);

	public record InviteBoardMemberRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[Required, StringLength(500), EmailAddress] string Email);

	public record ToggleTeacherDataRequest(bool CanAccessTeacherData);

	[HttpGet]
	public async Task<ActionResult<List<BoardMemberDto>>> GetAll(CancellationToken cancellationToken)
	{
		var members = await db.BoardMembers
			.AsNoTracking()
			.OrderBy(m => m.Name)
			.ToListAsync(cancellationToken);

		return Ok(members.Select(ToDto).ToList());
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<BoardMemberDto>> GetById(Guid id, CancellationToken cancellationToken)
	{
		var member = await db.BoardMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
		if (member is null) return NotFound();
		return Ok(ToDto(member));
	}

	[HttpGet("me")]
	[Authorize(Roles = Roles.Board)]
	public async Task<ActionResult<BoardMemberDto>> GetMe(CancellationToken cancellationToken)
	{
		var sub = User.FindFirstValue("sub");
		if (string.IsNullOrEmpty(sub)) return Unauthorized();

		var member = await db.BoardMembers
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(m => m.KeycloakSubject == sub, cancellationToken);

		if (member is null) return NotFound();
		return Ok(ToDto(member));
	}

	[HttpPost("invite")]
	public async Task<ActionResult<BoardMemberDto>> Invite(
		[FromBody] InviteBoardMemberRequest req,
		CancellationToken cancellationToken)
	{
		var member = new BoardMember
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name.Trim(),
			Email = req.Email.Trim().ToLowerInvariant(),
		};

		db.BoardMembers.Add(member);
		await db.SaveChangesAsync(cancellationToken);

		await invitationService.CreateAndSendAsync(member, cancellationToken);

		return CreatedAtAction(nameof(GetById), new { id = member.Id }, ToDto(member));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
	{
		var member = await db.BoardMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
		if (member is null) return NotFound();

		if (!string.IsNullOrEmpty(member.KeycloakSubject))
		{
			try
			{
				await keycloakAdmin.DeleteStaffUserAsync(member.KeycloakSubject, cancellationToken);
			}
			catch (KeycloakException)
			{
				// Log but don't fail — DB cleanup is more important
			}
		}

		db.BoardMembers.Remove(member);
		await db.SaveChangesAsync(cancellationToken);
		return NoContent();
	}

	[HttpPatch("{id:guid}/teacher-data-access")]
	public async Task<ActionResult<BoardMemberDto>> ToggleTeacherDataAccess(
		Guid id,
		[FromBody] ToggleTeacherDataRequest req,
		CancellationToken cancellationToken)
	{
		var member = await db.BoardMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
		if (member is null) return NotFound();

		member.CanAccessTeacherData = req.CanAccessTeacherData;
		await db.SaveChangesAsync(cancellationToken);
		return Ok(ToDto(member));
	}

	private static BoardMemberDto ToDto(BoardMember m) =>
		new(m.Id, m.Name, m.Email, m.CanAccessTeacherData, m.KeycloakSubject is not null, m.CreatedAt);
}
