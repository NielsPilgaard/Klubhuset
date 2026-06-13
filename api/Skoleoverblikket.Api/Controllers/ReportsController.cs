using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReportsController(ExcelReportBuilder excel, UvmTimetableService timetable) : ControllerBase
{
	/// <summary>GET /api/v1/reports/hours/staff.xlsx</summary>
	[HttpGet("hours/staff.xlsx")]
	public async Task<IActionResult> GetStaffHoursXlsx(CancellationToken cancellationToken)
	{
		var activeSlots = await excel.GetActiveSlotsAsync(cancellationToken);

		var teacherHours = activeSlots
			.GroupBy(s => (s.TeacherId, s.Teacher.Name, s.Teacher.Role))
			.Select(g => (g.Key.Name,
				 g.Key.Role,
				Hours: Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)));

		var aideHours = activeSlots
			.Where(s => s.AideId.HasValue)
			.GroupBy(s => (AideId: s.AideId!.Value, s.Aide!.Name, s.Aide.Role))
			.Select(g => (g.Key.Name,
				 g.Key.Role,
				Hours: Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2)));

		var rows = teacherHours.Concat(aideHours).OrderBy(r => r.Name).ToList();

		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("Timer pr. medarbejder");
		ws.Cell(1, 1).Value = "Navn";
		ws.Cell(1, 2).Value = "Rolle";
		ws.Cell(1, 3).Value = "Timer";
		ExcelReportBuilder.StyleHeader(ws.Row(1));

		for (var i = 0; i < rows.Count; i++)
		{
			ws.Cell(i + 2, 1).Value = rows[i].Name;
			ws.Cell(i + 2, 2).Value = ExcelReportBuilder.RoleLabel(rows[i].Role);
			ws.Cell(i + 2, 3).Value = rows[i].Hours;
		}

		ws.Columns().AdjustToContents();
		return ExcelReportBuilder.ToXlsx(wb, "timer-medarbejdere.xlsx");
	}

	/// <summary>GET /api/v1/reports/hours/courses.xlsx</summary>
	[HttpGet("hours/courses.xlsx")]
	public async Task<IActionResult> GetCourseHoursXlsx(CancellationToken cancellationToken)
	{
		var activeSlots = await excel.GetActiveSlotsAsync(cancellationToken);

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
		ExcelReportBuilder.StyleHeader(ws.Row(1));

		for (var i = 0; i < rows.Count; i++)
		{
			ws.Cell(i + 2, 1).Value = rows[i].ClassName;
			ws.Cell(i + 2, 2).Value = rows[i].CourseName;
			ws.Cell(i + 2, 3).Value = rows[i].Hours;
		}

		ws.Columns().AdjustToContents();
		return ExcelReportBuilder.ToXlsx(wb, "timer-fag.xlsx");
	}

	/// <summary>GET /api/v1/reports/schema.xlsx</summary>
	[HttpGet("schema.xlsx")]
	public async Task<IActionResult> GetSchemaXlsx(CancellationToken cancellationToken)
	{
		var activeSlots = await excel.GetActiveSlotsAsync(cancellationToken);

		var rows = activeSlots
			.OrderBy(s => s.Schema.Class.Name)
			.ThenBy(s => s.Weekday)
			.ThenBy(s => s.TimeSlot.StartTime)
			.Select(s => (
				ClassName: s.Schema.Class.Name,
				Day: ExcelReportBuilder.DayLabel(s.Weekday),
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
		ExcelReportBuilder.StyleHeader(ws.Row(1));

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
		return ExcelReportBuilder.ToXlsx(wb, "skema.xlsx");
	}

	/// <summary>GET /api/v1/reports/uvm-minimumstimetal.xlsx</summary>
	[HttpGet("uvm-minimumstimetal.xlsx")]
	public async Task<IActionResult> GetUvmMinimumstimetalXlsx(CancellationToken cancellationToken)
	{
		var timetal = timetable.Load();

		var activeSlots = await excel.GetActiveSlotsAsync(cancellationToken);

		var rows = activeSlots
			.Where(s =>
				s.Schema.Class.GradeLevel.HasValue &&
				s.Course.Category.HasValue &&
				s.Course.Category != SubjectCategory.Fri)
			.GroupBy(s => (
				ClassName: s.Schema.Class.Name,
				GradeLevel: s.Schema.Class.GradeLevel!.Value,
				Subject: s.Course.Category!.Value.ToString()))
			.Select(g =>
			{
				var weeklyHours = Math.Round(g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours), 2);
				var annualHours = Math.Round(weeklyHours * 40, 0);
				var vejledende = timetal.TryGetValue(g.Key.Subject, out var gradeMap) && gradeMap.TryGetValue(g.Key.GradeLevel, out var wh) ? wh : 0.0;
				var minimum = Math.Round(vejledende * 40, 0);
				var status = minimum == 0 ? "Ikke relevant"
					: annualHours >= minimum ? "Opfyldt"
					: $"Mangler {minimum - annualHours:0} timer";
				return (g.Key.ClassName, g.Key.GradeLevel, g.Key.Subject, weeklyHours, annualHours, minimum, status);
			})
			.OrderBy(r => r.GradeLevel).ThenBy(r => r.ClassName).ThenBy(r => r.Subject)
			.ToList();

		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet("UVM minimumstimetal");
		ws.Cell(1, 1).Value = "Klasse";
		ws.Cell(1, 2).Value = "Klassetrin";
		ws.Cell(1, 3).Value = "Fag";
		ws.Cell(1, 4).Value = "Planlagte timer (uge)";
		ws.Cell(1, 5).Value = "Estimerede årstimer";
		ws.Cell(1, 6).Value = "Minimumstimetal";
		ws.Cell(1, 7).Value = "Status";
		ExcelReportBuilder.StyleHeader(ws.Row(1));

		for (var i = 0; i < rows.Count; i++)
		{
			ws.Cell(i + 2, 1).Value = rows[i].ClassName;
			ws.Cell(i + 2, 2).Value = rows[i].GradeLevel;
			ws.Cell(i + 2, 3).Value = rows[i].Subject;
			ws.Cell(i + 2, 4).Value = rows[i].weeklyHours;
			ws.Cell(i + 2, 5).Value = rows[i].annualHours;
			ws.Cell(i + 2, 6).Value = rows[i].minimum;
			ws.Cell(i + 2, 7).Value = rows[i].status;
		}

		ws.Columns().AdjustToContents();
		return ExcelReportBuilder.ToXlsx(wb, "uvm-minimumstimetal.xlsx");
	}
}
