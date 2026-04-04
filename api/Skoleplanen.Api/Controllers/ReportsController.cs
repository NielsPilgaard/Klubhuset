using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "admin")]
public sealed class ReportsController(AppDbContext db) : ControllerBase
{
    // RFC 4180: wrap every field in double quotes and escape embedded quotes by doubling them.
    private static string CsvField(string? value)
    {
        var s = value ?? string.Empty;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static string RoleLabel(StaffRole role) => role switch
    {
        StaffRole.Teacher    => "Lærer",
        StaffRole.Aide       => "Pædagog",
        StaffRole.Substitute => "Vikar",
        _                    => role.ToString()
    };

    private static string DayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday    => "Mandag",
        DayOfWeek.Tuesday   => "Tirsdag",
        DayOfWeek.Wednesday => "Onsdag",
        DayOfWeek.Thursday  => "Torsdag",
        DayOfWeek.Friday    => "Fredag",
        _                   => day.ToString()
    };

    private async Task<List<SchemaSlot>> GetActiveSlotsAsync(CancellationToken ct) =>
        await db.SchemaSlots
            .AsNoTrackingWithIdentityResolution()
            .Where(s => s.Schema.IsActive)
            .Include(s => s.Course)
            .Include(s => s.Schema).ThenInclude(sc => sc.Class)
            .Include(s => s.TimeSlot)
            .Include(s => s.Teacher)
            .Include(s => s.Room)
            .Include(s => s.Aide)
            .ToListAsync(ct);

    /// <summary>GET /api/v1/reports/hours/staff.csv</summary>
    [HttpGet("hours/staff.csv")]
    public async Task<IActionResult> GetStaffHoursCsv(CancellationToken ct)
    {
        var activeSlots = await GetActiveSlotsAsync(ct);

        var teacherHours = activeSlots
            .GroupBy(s => (s.TeacherId, s.Teacher.Name, s.Teacher.Role))
            .Select(g => (
                Name: g.Key.Name,
                Role: g.Key.Role,
                Hours: Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)));

        var aideHours = activeSlots
            .Where(s => s.AideId.HasValue)
            .GroupBy(s => (AideId: s.AideId!.Value, s.Aide!.Name, s.Aide.Role))
            .Select(g => (
                Name: g.Key.Name,
                Role: g.Key.Role,
                Hours: Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)));

        var rows = teacherHours.Concat(aideHours)
            .OrderBy(r => r.Name)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\"Navn\",\"Rolle\",\"Timer\"");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvField(row.Name),
                CsvField(RoleLabel(row.Role)),
                CsvField(row.Hours.ToString("F2"))));
        }

        Response.Headers.Append("Content-Disposition", "attachment; filename=\"timer-medarbejdere.csv\"");
        return Content(sb.ToString(), "text/csv");
    }

    /// <summary>GET /api/v1/reports/hours/courses.csv</summary>
    [HttpGet("hours/courses.csv")]
    public async Task<IActionResult> GetCourseHoursCsv(CancellationToken ct)
    {
        var activeSlots = await GetActiveSlotsAsync(ct);

        var rows = activeSlots
            .GroupBy(s => (s.Schema.Class.Name, s.Course.Name))
            .Select(g => (
                ClassName: g.Key.Item1,
                CourseName: g.Key.Item2,
                Hours: Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)))
            .OrderBy(r => r.ClassName)
            .ThenBy(r => r.CourseName)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\"Klasse\",\"Fag\",\"Timer\"");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvField(row.ClassName),
                CsvField(row.CourseName),
                CsvField(row.Hours.ToString("F2"))));
        }

        Response.Headers.Append("Content-Disposition", "attachment; filename=\"timer-fag.csv\"");
        return Content(sb.ToString(), "text/csv");
    }

    /// <summary>GET /api/v1/reports/schema.csv</summary>
    [HttpGet("schema.csv")]
    public async Task<IActionResult> GetSchemaCsv(CancellationToken ct)
    {
        var activeSlots = await GetActiveSlotsAsync(ct);

        var rows = activeSlots
            .OrderBy(s => s.Schema.Class.Name)
            .ThenBy(s => s.Weekday)
            .ThenBy(s => s.TimeSlot.StartTime)
            .Select(s => (
                ClassName: s.Schema.Class.Name,
                Day: DayLabel(s.Weekday),
                Start: s.TimeSlot.StartTime.ToString("HH:mm"),
                End: s.TimeSlot.EndTime.ToString("HH:mm"),
                Course: s.Course.Name,
                Teacher: s.Teacher.Name,
                Room: s.Room?.Name))
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\"Klasse\",\"Dag\",\"Start\",\"Slut\",\"Fag\",\"Lærer\",\"Lokale\"");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvField(row.ClassName),
                CsvField(row.Day),
                CsvField(row.Start),
                CsvField(row.End),
                CsvField(row.Course),
                CsvField(row.Teacher),
                CsvField(row.Room)));
        }

        Response.Headers.Append("Content-Disposition", "attachment; filename=\"skema.csv\"");
        return Content(sb.ToString(), "text/csv");
    }
}
