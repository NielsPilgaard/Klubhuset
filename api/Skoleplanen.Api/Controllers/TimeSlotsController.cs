using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Domain;
using Skoleplanen.Api.Tenancy;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class TimeSlotsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    public record BreakDto(Guid Id, TimeOnly StartTime, int DurationMinutes);
    public record TemplateDto(Guid Id, int LessonDurationMinutes, TimeOnly DayStartTime, TimeOnly DayEndTime,
        string ActiveDays, IReadOnlyList<BreakDto> Breaks);

    public record UpsertBreakRequest(TimeOnly StartTime, int DurationMinutes);
    public record UpsertTemplateRequest(int LessonDurationMinutes, TimeOnly DayStartTime, TimeOnly DayEndTime,
        string ActiveDays, IReadOnlyList<UpsertBreakRequest> Breaks);

    [HttpGet("time-slot-template")]
    public async Task<ActionResult<TemplateDto>> GetTemplate(CancellationToken ct)
    {
        var t = await db.TimeSlotTemplates
            .AsNoTrackingWithIdentityResolution()
            .Include(t => t.Breaks)
            .FirstOrDefaultAsync(ct);
        if (t is null) return NotFound();
        return Ok(ToTemplateDto(t));
    }

    [HttpPut("time-slot-template")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<TemplateDto>> UpsertTemplate([FromBody] UpsertTemplateRequest req, CancellationToken ct)
    {
        var t = await db.TimeSlotTemplates.Include(t => t.Breaks).FirstOrDefaultAsync(ct);
        if (t is null)
        {
            t = new TimeSlotTemplate { Id = Guid.NewGuid(), TenantId = tenant.TenantId };
            db.TimeSlotTemplates.Add(t);
        }

        t.LessonDurationMinutes = req.LessonDurationMinutes;
        t.DayStartTime = req.DayStartTime;
        t.DayEndTime = req.DayEndTime;
        t.ActiveDays = req.ActiveDays;

        // Replace breaks
        db.TimeSlotTemplateBreaks.RemoveRange(t.Breaks);
        t.Breaks = req.Breaks.Select(b => new TimeSlotTemplateBreak
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            TimeSlotTemplateId = t.Id,
            StartTime = b.StartTime,
            DurationMinutes = b.DurationMinutes,
        }).ToList();

        await db.SaveChangesAsync(ct);
        return Ok(ToTemplateDto(t));
    }

    public record TimeSlotDto(Guid Id, Guid? ClassId, int SortOrder, TimeOnly StartTime, TimeOnly EndTime, string? Label);
    public record UpsertTimeSlotRequest(int SortOrder, TimeOnly StartTime, TimeOnly EndTime, string? Label);

    [HttpGet("classes/{classId:guid}/time-slots")]
    public async Task<ActionResult<List<TimeSlotDto>>> GetForClass(Guid classId, CancellationToken ct)
    {
        var slots = await db.TimeSlots
            .AsNoTracking()
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label))
            .ToListAsync(ct);
        return Ok(slots);
    }

    [HttpPut("classes/{classId:guid}/time-slots")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<TimeSlotDto>>> ReplaceForClass(Guid classId, [FromBody] IReadOnlyList<UpsertTimeSlotRequest> req, CancellationToken ct)
    {
        // Verify class belongs to tenant
        var exists = await db.Classes.AnyAsync(c => c.Id == classId, ct);
        if (!exists) return NotFound();

        var existing = await db.TimeSlots.Where(s => s.ClassId == classId).ToListAsync(ct);
        db.TimeSlots.RemoveRange(existing);

        var newSlots = req.Select((r, i) => new TimeSlot
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            ClassId = classId,
            SortOrder = r.SortOrder,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            Label = r.Label,
        }).ToList();

        db.TimeSlots.AddRange(newSlots);
        await db.SaveChangesAsync(ct);

        var result = newSlots.Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label));
        return Ok(result);
    }

    [HttpGet("time-slots")]
    public async Task<ActionResult<List<TimeSlotDto>>> GetSchoolLevelSlots(CancellationToken ct)
    {
        var slots = await db.TimeSlots
            .AsNoTracking()
            .Where(s => s.ClassId == null)
            .OrderBy(s => s.SortOrder)
            .Select(s => new TimeSlotDto(s.Id, s.ClassId, s.SortOrder, s.StartTime, s.EndTime, s.Label))
            .ToListAsync(ct);
        return Ok(slots);
    }

    private static TemplateDto ToTemplateDto(TimeSlotTemplate t) => new(
        t.Id, t.LessonDurationMinutes, t.DayStartTime, t.DayEndTime, t.ActiveDays,
        t.Breaks.OrderBy(b => b.StartTime).Select(b => new BreakDto(b.Id, b.StartTime, b.DurationMinutes)).ToList());
}
