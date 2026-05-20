using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/students")]
[Authorize(Roles = Roles.Admin)]
public sealed class StudentsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
	public record StudentDto(Guid Id, string Name, Guid ClassId, string ClassName, DateTimeOffset CreatedAt);
	public record UpsertStudentRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		Guid ClassId);

	[HttpGet]
	public async Task<ActionResult<List<StudentDto>>> GetAll([FromQuery] Guid? classId, CancellationToken ct)
	{
		var query = db.Students.AsNoTracking().Include(s => s.Class).AsQueryable();

		if (classId.HasValue)
		{
			query = query.Where(s => s.ClassId == classId.Value);
		}

		var students = await query
			.OrderBy(s => s.Class.Name)
			.ThenBy(s => s.Name)
			.Select(s => new StudentDto(s.Id, s.Name, s.ClassId, s.Class.Name, s.CreatedAt))
			.ToListAsync(ct);

		return Ok(students);
	}

	[HttpPost]
	public async Task<ActionResult<StudentDto>> Create([FromBody] UpsertStudentRequest req, CancellationToken ct)
	{
		var classExists = await db.Classes.AnyAsync(c => c.Id == req.ClassId, ct);
		if (!classExists)
		{
			return ValidationProblem("ClassId does not reference a valid class.");
		}

		var student = new Student
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			ClassId = req.ClassId,
		};

		db.Students.Add(student);
		await db.SaveChangesAsync(ct);

		var className = await db.Classes.Where(c => c.Id == req.ClassId).Select(c => c.Name).FirstAsync(ct);
		return CreatedAtAction(nameof(GetAll), new StudentDto(student.Id, student.Name, student.ClassId, className, student.CreatedAt));
	}

	[HttpPut("{id:guid}")]
	public async Task<ActionResult<StudentDto>> Update(Guid id, [FromBody] UpsertStudentRequest req, CancellationToken ct)
	{
		var student = await db.Students.Include(s => s.Class).FirstOrDefaultAsync(s => s.Id == id, ct);
		if (student is null)
		{
			return NotFound();
		}

		var classExists = await db.Classes.AnyAsync(c => c.Id == req.ClassId, ct);
		if (!classExists)
		{
			return ValidationProblem("ClassId does not reference a valid class.");
		}

		student.Name = req.Name;
		student.ClassId = req.ClassId;
		await db.SaveChangesAsync(ct);

		var className = await db.Classes.Where(c => c.Id == req.ClassId).Select(c => c.Name).FirstAsync(ct);
		return Ok(new StudentDto(student.Id, student.Name, student.ClassId, className, student.CreatedAt));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
		if (student is null)
		{
			return NotFound();
		}

		db.Students.Remove(student);
		await db.SaveChangesAsync(ct);
		return NoContent();
	}
}
