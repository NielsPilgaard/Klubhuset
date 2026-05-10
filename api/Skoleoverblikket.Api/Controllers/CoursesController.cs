using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/courses")]
[Authorize]
public sealed class CoursesController(AppDbContext db, ITenantContext tenant) : ControllerBase
{

	public record CourseDto(Guid Id, string Name, string? Description, string? Color, SubjectCategory? Category);
	public record UpsertCourseRequest(
		[Required, StringLength(200, MinimumLength = 1)] string Name,
		[StringLength(2000)] string? Description,
		[StringLength(7)] string? Color,
		SubjectCategory? Category);

	[HttpGet]
	public async Task<ActionResult<List<CourseDto>>> GetAll(CancellationToken ct)
	{
		var courses = await db.Courses
			.AsNoTracking()
			.OrderBy(c => c.Name)
			.Select(c => new CourseDto(c.Id, c.Name, c.Description, c.Color, c.Category))
			.ToListAsync(ct);

		return Ok(courses);
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken ct)
	{
		var course = await db.Courses
							 .AsNoTracking()
							 .Where(x => x.Id == id)
							 .Select(x => new CourseDto(x.Id, x.Name, x.Description, x.Color, x.Category))
							 .FirstOrDefaultAsync(ct);

		return course is null
				   ? NotFound()
				   : Ok(course);
	}

	[HttpPost]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<CourseDto>> Create([FromBody] UpsertCourseRequest req, CancellationToken ct)
	{
		var course = new Course
		{
			Id = Guid.NewGuid(),
			TenantId = tenant.TenantId,
			Name = req.Name,
			Description = req.Description,
			Color = req.Color,
			Category = req.Category,
		};

		db.Courses.Add(course);
		await db.SaveChangesAsync(ct);

		return CreatedAtAction(nameof(GetById), new { id = course.Id },
			new CourseDto(course.Id, course.Name, course.Description, course.Color, course.Category));
	}

	[HttpPut("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult<CourseDto>> Update(Guid id, [FromBody] UpsertCourseRequest req, CancellationToken ct)
	{
		var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
		if (course is null)
		{
			return NotFound();
		}

		course.Name = req.Name;
		course.Description = req.Description;
		course.Color = req.Color;
		course.Category = req.Category;

		await db.SaveChangesAsync(ct);

		return Ok(new CourseDto(course.Id, course.Name, course.Description, course.Color, course.Category));
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Roles = "admin")]
	public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
	{
		var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
		if (course is null)
		{
			return NotFound();
		}

		db.Courses.Remove(course);

		await db.SaveChangesAsync(ct);

		return NoContent();
	}
}
