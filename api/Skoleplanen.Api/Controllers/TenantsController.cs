using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController(AppDbContext db) : ControllerBase
{
	public record CreateTenantRequest(
		[Required]
		[MinLength(1)]
		[MaxLength(100)]
		string Name,
		[EmailAddress] string? ContactEmail);

	public record TenantDto(Guid Id, string Name, string? ContactEmail);

	/// <summary>
	/// Create a new tenant (school). Called during school signup.
	/// </summary>
	[HttpPost]
	[Authorize]
	public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
	{
		var school = new School
		{
			Id = Guid.NewGuid(),
			Name = req.Name,
			ContactEmail = req.ContactEmail,
		};

		db.Schools.Add(school);
		await db.SaveChangesAsync(ct);

		return Ok(new TenantDto(school.Id, school.Name, school.ContactEmail));
	}
}
