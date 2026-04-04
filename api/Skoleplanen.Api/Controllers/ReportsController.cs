using ClosedXML.Excel;
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
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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

    private static void StyleHeader(IXLRow row)
    {
        row.Style.Font.Bold = true;
        row.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        row.Style.Font.FontColor = XLColor.White;
    }

    private static FileStreamResult ToXlsx(XLWorkbook wb, string filename)
    {
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return new FileStreamResult(stream, XlsxMime) { FileDownloadName = filename };
    }

    /// <summary>GET /api/v1/reports/hours/staff.xlsx</summary>
    [HttpGet("hours/staff.xlsx")]
    public async Task<IActionResult> GetStaffHoursXlsx(CancellationToken ct)
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

        var rows = teacherHours.Concat(aideHours).OrderBy(r => r.Name).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Timer pr. medarbejder");
        ws.Cell(1, 1).Value = "Navn";
        ws.Cell(1, 2).Value = "Rolle";
        ws.Cell(1, 3).Value = "Timer";
        StyleHeader(ws.Row(1));

        for (var i = 0; i < rows.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].Name;
            ws.Cell(i + 2, 2).Value = RoleLabel(rows[i].Role);
            ws.Cell(i + 2, 3).Value = rows[i].Hours;
        }

        ws.Columns().AdjustToContents();
        return ToXlsx(wb, "timer-medarbejdere.xlsx");
    }

    /// <summary>GET /api/v1/reports/hours/courses.xlsx</summary>
    [HttpGet("hours/courses.xlsx")]
    public async Task<IActionResult> GetCourseHoursXlsx(CancellationToken ct)
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

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Timer pr. fag");
        ws.Cell(1, 1).Value = "Klasse";
        ws.Cell(1, 2).Value = "Fag";
        ws.Cell(1, 3).Value = "Timer";
        StyleHeader(ws.Row(1));

        for (var i = 0; i < rows.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].ClassName;
            ws.Cell(i + 2, 2).Value = rows[i].CourseName;
            ws.Cell(i + 2, 3).Value = rows[i].Hours;
        }

        ws.Columns().AdjustToContents();
        return ToXlsx(wb, "timer-fag.xlsx");
    }

    /// <summary>GET /api/v1/reports/schema.xlsx</summary>
    [HttpGet("schema.xlsx")]
    public async Task<IActionResult> GetSchemaXlsx(CancellationToken ct)
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
                Room: s.Room?.Name ?? string.Empty))
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Komplet skema");
        ws.Cell(1, 1).Value = "Klasse";
        ws.Cell(1, 2).Value = "Dag";
        ws.Cell(1, 3).Value = "Start";
        ws.Cell(1, 4).Value = "Slut";
        ws.Cell(1, 5).Value = "Fag";
        ws.Cell(1, 6).Value = "Lærer";
        ws.Cell(1, 7).Value = "Lokale";
        StyleHeader(ws.Row(1));

        for (var i = 0; i < rows.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].ClassName;
            ws.Cell(i + 2, 2).Value = rows[i].Day;
            ws.Cell(i + 2, 3).Value = rows[i].Start;
            ws.Cell(i + 2, 4).Value = rows[i].End;
            ws.Cell(i + 2, 5).Value = rows[i].Course;
            ws.Cell(i + 2, 6).Value = rows[i].Teacher;
            ws.Cell(i + 2, 7).Value = rows[i].Room;
        }

        ws.Columns().AdjustToContents();
        return ToXlsx(wb, "skema.xlsx");
    }
}
