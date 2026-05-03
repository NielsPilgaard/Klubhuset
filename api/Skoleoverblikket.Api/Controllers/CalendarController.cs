using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Tenancy;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/calendar")]
[Authorize]
public sealed class CalendarController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    public record CalendarEntryDto(Guid Id, CalendarEntryType Type, string Title, DateOnly StartDate, DateOnly EndDate);
    public record CreateCalendarEntryRequest([Required][MinLength(1)] string Title, CalendarEntryType Type, DateOnly StartDate, DateOnly EndDate);
    public record UpdateCalendarEntryRequest([Required][MinLength(1)] string Title, CalendarEntryType Type, DateOnly StartDate, DateOnly EndDate);
    public record DefaultHolidayDto(string Title, CalendarEntryType Type, DateOnly StartDate, DateOnly EndDate);

    [HttpGet]
    public async Task<ActionResult<List<CalendarEntryDto>>> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        var query = db.CalendarEntries.AsNoTracking();

        if (year.HasValue)
        {
            query = query.Where(e => e.StartDate.Year == year.Value || e.EndDate.Year == year.Value);
        }

        var entries = await query
            .OrderBy(e => e.StartDate)
            .Select(e => new CalendarEntryDto(e.Id, e.Type, e.Title, e.StartDate, e.EndDate))
            .ToListAsync(ct);

        return Ok(entries);
    }

    [HttpGet("defaults")]
    public ActionResult<List<DefaultHolidayDto>> GetDefaults([FromQuery] int? year)
    {
        // year is the school start year (e.g. 2025 = 2025/2026)
        // Default to current school start year
        var today = DateOnly.FromDateTime(DateTime.Today);
        var targetYear = year ?? (today.Month >= 8 ? today.Year : today.Year - 1);
        var defaults = ComputeDefaultHolidays(targetYear);
        return Ok(defaults);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CalendarEntryDto>> Create([FromBody] CreateCalendarEntryRequest req, CancellationToken ct)
    {
        if (req.StartDate > req.EndDate)
		{
			return Problem("StartDate skal være før eller lig EndDate", statusCode: 400);
		}

		var entry = new CalendarEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Type = req.Type,
            Title = req.Title,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
        };
        db.CalendarEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new CalendarEntryDto(entry.Id, entry.Type, entry.Title, entry.StartDate, entry.EndDate));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CalendarEntryDto>> Update(Guid id, [FromBody] UpdateCalendarEntryRequest req, CancellationToken ct)
    {
        if (req.StartDate > req.EndDate)
		{
			return Problem("StartDate skal være før eller lig EndDate", statusCode: 400);
		}

		var entry = await db.CalendarEntries.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entry is null)
		{
			return NotFound();
		}

		entry.Type = req.Type;
        entry.Title = req.Title;
        entry.StartDate = req.StartDate;
        entry.EndDate = req.EndDate;
        await db.SaveChangesAsync(ct);
        return Ok(new CalendarEntryDto(entry.Id, entry.Type, entry.Title, entry.StartDate, entry.EndDate));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await db.CalendarEntries.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entry is null)
		{
			return NotFound();
		}

		db.CalendarEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static List<DefaultHolidayDto> ComputeDefaultHolidays(int year)
    {
        // year is treated as the school start year (e.g. 2025 = school year 2025/2026)
        var schoolStartYear = year;
        var schoolEndYear = schoolStartYear + 1;

        var easter = ComputeEaster(schoolEndYear);

        // Palm Sunday = Easter - 7, Easter Monday = Easter + 1
        var palmSunday = easter.AddDays(-7);
        var easterMonday = easter.AddDays(1);

        // Kristi Himmelfartsdag = Easter + 39
        var ascension = easter.AddDays(39);

        // Pinse: Whit Sunday = Easter + 49, Whit Monday = Easter + 50
        // Show Fri before + Mon as Ferie
        var whitFriday = easter.AddDays(48); // Friday before Whit Sunday
        var whitMonday = easter.AddDays(50);

        // Efterårsferie: ISO week 42 of school start year
        var efteraarStart = DateOnly.FromDateTime(ISOWeek.ToDateTime(schoolStartYear, 42, DayOfWeek.Monday));
        var efteraarEnd = efteraarStart.AddDays(4);

        // Juleferie: Dec 22 through Jan 2
        var juleStart = new DateOnly(schoolStartYear, 12, 22);
        var juleEnd = new DateOnly(schoolEndYear, 1, 2);

        // Vinterferie: ISO week 7 of school end year
        var vinterStart = DateOnly.FromDateTime(ISOWeek.ToDateTime(schoolEndYear, 7, DayOfWeek.Monday));
        var vinterEnd = vinterStart.AddDays(4);

        // Sommerferie: Jun 26 through Aug 7
        var sommerStart = new DateOnly(schoolEndYear, 6, 26);
        var sommerEnd = new DateOnly(schoolEndYear, 8, 7);

        // Grundlovsdag: June 5
        var grundlovsdag = new DateOnly(schoolEndYear, 6, 5);

        return
        [
            new DefaultHolidayDto("Efterårsferie", CalendarEntryType.Ferie, efteraarStart, efteraarEnd),
            new DefaultHolidayDto("Juleferie", CalendarEntryType.Ferie, juleStart, juleEnd),
            new DefaultHolidayDto("Vinterferie", CalendarEntryType.Ferie, vinterStart, vinterEnd),
            new DefaultHolidayDto("Påskeferie", CalendarEntryType.Ferie, palmSunday, easterMonday),
            new DefaultHolidayDto("Kristi Himmelfartsdag", CalendarEntryType.Lukkedag, ascension, ascension),
            new DefaultHolidayDto("Pinse", CalendarEntryType.Ferie, whitFriday, whitMonday),
            new DefaultHolidayDto("Grundlovsdag", CalendarEntryType.Lukkedag, grundlovsdag, grundlovsdag),
            new DefaultHolidayDto("Sommerferie", CalendarEntryType.Ferie, sommerStart, sommerEnd),
        ];
    }

    private static DateOnly ComputeEaster(int year)
    {
        // Anonymous Gregorian algorithm
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }
}
