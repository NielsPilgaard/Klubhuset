using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/parents")]
[Authorize(Roles = Roles.Admin)]
public sealed class ParentsController(
	AppDbContext db,
	ITenantContext tenant,
	ParentInvitationService invitationService,
	KeycloakAdminService keycloak,
	ILogger<ParentsController> logger) : ControllerBase
{
	public record ParentDto(Guid Id, string Name, string Email, string? Phone, string? Address, string? PostalCode, string? City, IReadOnlyList<StudentRefDto> Students, bool HasAccount, DateTimeOffset CreatedAt, bool AdresseBeskyttet);
	public record StudentRefDto(Guid Id, string Name, Guid ClassId, string ClassName);
	public record InviteParentRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[Required, EmailAddress, StringLength(500)] string Email,
		[Required] IReadOnlyList<Guid> StudentIds);

	[HttpGet]
	public async Task<ActionResult<List<ParentDto>>> GetAll(CancellationToken cancellationToken)
	{
		var parents = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students).ThenInclude(s => s.Class)
			.OrderBy(p => p.Name)
			.ToListAsync(cancellationToken);

		return Ok(parents.Select(ToDto).ToList());
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ParentDto>> GetById(Guid id, CancellationToken cancellationToken)
	{
		var parent = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students).ThenInclude(s => s.Class)
			.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

		return parent is null ? NotFound() : Ok(ToDto(parent));
	}

	[HttpPost("invite")]
	public async Task<ActionResult<ParentDto>> Invite([FromBody] InviteParentRequest req, CancellationToken cancellationToken)
	{
		// Validate all student IDs belong to this tenant
		var students = await db.Students
			.Where(s => req.StudentIds.Contains(s.Id))
			.ToListAsync(cancellationToken);

		if (students.Count != req.StudentIds.Count)
		{
			return ValidationProblem("One or more student IDs were not found.");
		}

		var parent = new Parent
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Email = req.Email,
		};

		foreach (var student in students)
		{
			parent.Students.Add(student);
		}

		db.Parents.Add(parent);
		await db.SaveChangesAsync(cancellationToken);

		try
		{
			await invitationService.CreateAndSendAsync(parent, cancellationToken);
		}
		catch (InvalidOperationException ex)
		{
			return Problem(title: "Konfigurationsfejl", detail: ex.Message, statusCode: 500);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send parent invitation email to {Email}", req.Email);
			return Problem(title: "Kunne ikke sende invitation", detail: "Der opstod en fejl. Prøv igen.", statusCode: 502);
		}

		var withStudents = await db.Parents
			.AsNoTracking()
			.Include(p => p.Students).ThenInclude(s => s.Class)
			.FirstAsync(p => p.Id == parent.Id, cancellationToken);

		return CreatedAtAction(nameof(GetById), new { id = parent.Id }, ToDto(withStudents));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
	{
		var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
		if (parent is null)
		{
			return NotFound();
		}

		if (!string.IsNullOrWhiteSpace(parent.KeycloakSubject))
		{
			try
			{
				await keycloak.DeleteStaffUserAsync(parent.KeycloakSubject, cancellationToken);
			}
			catch (KeycloakException ex)
			{
				logger.LogError(ex, "Failed to delete Keycloak account for parent {ParentId}", id);
			}
		}

		db.Parents.Remove(parent);
		await db.SaveChangesAsync(cancellationToken);
		return NoContent();
	}

	[HttpPost("{id:guid}/students/{studentId:guid}")]
	public async Task<ActionResult> LinkStudent(Guid id, Guid studentId, CancellationToken cancellationToken)
	{
		var parent = await db.Parents.Include(p => p.Students).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
		if (parent is null)
		{
			return NotFound();
		}

		var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);
		if (student is null)
		{
			return NotFound();
		}

		if (parent.Students.Any(s => s.Id == studentId))
		{
			return Conflict();
		}

		parent.Students.Add(student);
		await db.SaveChangesAsync(cancellationToken);
		return NoContent();
	}

	[HttpDelete("{id:guid}/students/{studentId:guid}")]
	public async Task<ActionResult> UnlinkStudent(Guid id, Guid studentId, CancellationToken cancellationToken)
	{
		var parent = await db.Parents.Include(p => p.Students).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
		if (parent is null)
		{
			return NotFound();
		}

		var student = parent.Students.FirstOrDefault(s => s.Id == studentId);
		if (student is null)
		{
			return NotFound();
		}

		parent.Students.Remove(student);
		await db.SaveChangesAsync(cancellationToken);
		return NoContent();
	}

	[HttpPatch("{id:guid}/adresse-beskyttelse")]
	public async Task<ActionResult> SetAdresseBeskyttelse(Guid id, [FromBody] AdresseBeskyttelseRequest req, CancellationToken cancellationToken)
	{
		var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
		if (parent is null)
		{
			return NotFound();
		}

		parent.AdresseBeskyttet = req.AdresseBeskyttet;
		await db.SaveChangesAsync(cancellationToken);
		return NoContent();
	}

	public record AdresseBeskyttelseRequest(bool AdresseBeskyttet);

	public record UpdateParentContactRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[StringLength(50)] string? Phone,
		[StringLength(500)] string? Address,
		[StringLength(10)] string? PostalCode,
		[StringLength(100)] string? City);

	[HttpPatch("{id:guid}/contact")]
	public async Task<ActionResult> UpdateContact(Guid id, [FromBody] UpdateParentContactRequest req, CancellationToken cancellationToken)
	{
		var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
		if (parent is null)
		{
			return NotFound();
		}

		parent.Name = req.Name.Trim();
		parent.Phone = req.Phone;
		parent.Address = req.Address;
		parent.PostalCode = req.PostalCode;
		parent.City = req.City;
		await db.SaveChangesAsync(cancellationToken);
		return NoContent();
	}

	private static ParentDto ToDto(Parent p) => new(
		p.Id,
		p.Name,
		p.Email,
		p.Phone,
		p.Address,
		p.PostalCode,
		p.City,
		p.Students.Select(s => new StudentRefDto(s.Id, s.Name, s.ClassId, s.Class?.Name ?? string.Empty)).ToList(),
		p.KeycloakSubject is not null,
		p.CreatedAt,
		p.AdresseBeskyttet);
}
