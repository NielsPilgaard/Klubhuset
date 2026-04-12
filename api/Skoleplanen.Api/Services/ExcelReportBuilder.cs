using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleplanen.Api.Data;
using Skoleplanen.Api.Models;

namespace Skoleplanen.Api.Services;

public sealed class ExcelReportBuilder(AppDbContext db)
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<List<SchemaSlot>> GetActiveSlotsAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.SchemaSlots
            .AsNoTrackingWithIdentityResolution()
            .Where(s => s.Schema.StartDate <= today && s.Schema.EndDate >= today)
            .Include(s => s.Course)
            .Include(s => s.Schema).ThenInclude(sc => sc.Class)
            .Include(s => s.TimeSlot)
            .Include(s => s.Teacher)
            .Include(s => s.Room)
            .Include(s => s.Aide)
            .ToListAsync(ct);
    }

    public static void StyleHeader(IXLRow row)
    {
        row.Style.Font.Bold = true;
        row.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
        row.Style.Font.FontColor = XLColor.White;
    }

    public static FileStreamResult ToXlsx(XLWorkbook wb, string filename)
    {
        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return new FileStreamResult(stream, XlsxMime) { FileDownloadName = filename };
    }

    public static string RoleLabel(StaffRole role) => role switch
    {
        StaffRole.Teacher    => "Lærer",
        StaffRole.Aide       => "Pædagog",
        StaffRole.Substitute => "Vikar",
        _                    => role.ToString()
    };

    public static string DayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday    => "Mandag",
        DayOfWeek.Tuesday   => "Tirsdag",
        DayOfWeek.Wednesday => "Onsdag",
        DayOfWeek.Thursday  => "Torsdag",
        DayOfWeek.Friday    => "Fredag",
        _                   => day.ToString()
    };
}
