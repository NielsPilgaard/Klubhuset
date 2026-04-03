using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Domain;
using Skoleplanen.Api.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/courses")]
[Authorize]
public sealed class CoursesController(AppDbContext db, ITenantContext tenant) : ControllerBase
{

public record CourseDto(Guid Id, string Name, string? Description);
public record UpsertCourseRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(2000)] string? Description);

    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetAll(CancellationToken ct)
    {
        var courses = await db.Courses
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CourseDto(c.Id, c.Name, c.Description))
            .ToListAsync(ct);
        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken ct)
    {
        var c = await db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        return Ok(new CourseDto(c.Id, c.Name, c.Description));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourseDto>> Create([FromBody] UpsertCourseRequest req, CancellationToken ct)
    {
        var c = new Course
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Name = req.Name,
            Description = req.Description,
        };
        db.Courses.Add(c);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = c.Id },
            new CourseDto(c.Id, c.Name, c.Description));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CourseDto>> Update(Guid id, [FromBody] UpsertCourseRequest req, CancellationToken ct)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        c.Name = req.Name;
        c.Description = req.Description;
        await db.SaveChangesAsync(ct);
        return Ok(new CourseDto(c.Id, c.Name, c.Description));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        db.Courses.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
