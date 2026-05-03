using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController(AppDbContext db, KeycloakAdminService keycloakAdmin) : ControllerBase
{
	public record CreateTenantRequest(
		[Required]
		[MinLength(1)]
		[MaxLength(100)]
		string Name,
		[Required]
		[EmailAddress]
		string AdminEmail,
		[Required]
		[MinLength(1)]
		string AdminFirstName,
		[Required]
		[MinLength(1)]
		string AdminLastName,
		[Required]
		[MinLength(8)]
		string AdminPassword);

	public record TenantCreatedDto(Guid Id, string Name, string AdminEmail, string AccessToken, string? RefreshToken, int ExpiresIn);

	/// <summary>
	/// Create a new tenant (school) and its first admin user.
	/// Returns a JWT so the frontend can initialise Keycloak immediately — no separate login redirect needed.
	/// Called anonymously during school signup.
	/// </summary>
	[HttpPost]
	[AllowAnonymous]
	public async Task<ActionResult<TenantCreatedDto>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
	{
		var school = new School
		{
			Id = Guid.NewGuid(),
			Name = req.Name,
			ContactEmail = req.AdminEmail,
		};

		db.Schools.Add(school);

		string keycloakSubject;
		try
		{
			keycloakSubject = await keycloakAdmin.CreateAdminUserAsync(
				email: req.AdminEmail,
				firstName: req.AdminFirstName,
				lastName: req.AdminLastName,
				password: req.AdminPassword,
				tenantId: school.Id,
				ct);
		}
		catch (KeycloakException ex)
		{
			return Problem(
				title: "Kunne ikke oprette brugerkonto",
				detail: ex.Message,
				statusCode: 502);
		}

		db.Staff.Add(new Staff
		{
			Id = Guid.NewGuid(),
			TenantId = school.Id,
			Name = $"{req.AdminFirstName} {req.AdminLastName}".Trim(),
			Email = req.AdminEmail,
			Role = StaffRole.Teacher,
			KeycloakSubject = keycloakSubject,
		});

		await db.SaveChangesAsync(ct);

		TokenResponse token;
		try
		{
			token = await keycloakAdmin.GetTokenForUserAsync(req.AdminEmail, req.AdminPassword, ct);
		}
		catch (Exception ex)
		{
			return Problem(
				title: "Skole oprettet, men login fejlede",
				detail: ex.Message,
				statusCode: 502);
		}

		return Ok(new TenantCreatedDto(school.Id, school.Name, school.ContactEmail, token.AccessToken, token.RefreshToken, token.ExpiresIn));
	}
}
