using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skoleoverblikket.Api.Auth;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.Controllers;

[ApiController]
[Route("api/v1/staa-maal-med")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Board}")]
public sealed class StaaMaalMedController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
	private static Dictionary<string, Dictionary<int, double>>? _timetal;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
	private static readonly Lock _lock = new();

	private Dictionary<string, Dictionary<int, double>> LoadTimetal()
	{
		if (_timetal is not null)
		{
			return _timetal;
		}

		lock (_lock)
		{
			if (_timetal is not null)
			{
				return _timetal;
			}

			var dataPath = Path.Combine(env.ContentRootPath, "data", "uvm-timetal");
			if (!Directory.Exists(dataPath))
			{
				dataPath = Path.Combine(AppContext.BaseDirectory, "data", "uvm-timetal");
			}

			var files = Directory.GetFiles(dataPath, "*.json").OrderDescending().ToArray();
			if (files.Length == 0)
			{
				return _timetal = [];
			}

			// Pick file matching current school year (e.g. 2025-2026.json for Aug 2025 – Jul 2026)
			var now = DateTime.UtcNow;
			var schoolYearStart = now.Month >= 8 ? now.Year : now.Year - 1;
			var targetName = $"{schoolYearStart}-{schoolYearStart + 1}.json";
			var match = files.FirstOrDefault(f => Path.GetFileName(f) == targetName) ?? files[0];

			var json = System.IO.File.ReadAllText(match);
			_timetal = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, double>>>(json,
				JsonOptions) ?? [];
			return _timetal;
		}
	}

	public record SubjectCoverageDto(string Category, double WeeklyHours, string Status);
	public record ClassCoverageDto(Guid ClassId, string ClassName, int GradeLevel, List<SubjectCoverageDto> Subjects);
	public record CoverageResponseDto(List<ClassCoverageDto> Classes);

	[HttpGet("coverage")]
	public async Task<ActionResult<CoverageResponseDto>> GetCoverage(CancellationToken cancellationToken)
	{
		var timetal = LoadTimetal();

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var activeSlots = await db.SchemaSlots
			.AsNoTrackingWithIdentityResolution()
			.Where(s => s.Schema.StartDate <= today && s.Schema.EndDate >= today)
			.Include(s => s.Course)
			.Include(s => s.Schema).ThenInclude(sc => sc.Class)
			.Include(s => s.TimeSlot)
			.ToListAsync(cancellationToken);

		var classes = activeSlots
			.Where(s => s.Schema.Class.GradeLevel.HasValue && s.Course.Category.HasValue && s.Course.Category != SubjectCategory.Fri)
			.GroupBy(s => (s.Schema.ClassId, s.Schema.Class.Name, GradeLevel: s.Schema.Class.GradeLevel!.Value))
			.Select(classGroup =>
			{
				var gradeLevel = classGroup.Key.GradeLevel;
				var hoursPerCategory = classGroup
					.GroupBy(s => s.Course.Category!.Value)
					.ToDictionary(g => g.Key, g => g.Sum(s => (s.TimeSlot.EndTime - s.TimeSlot.StartTime).TotalHours));

				var subjects = new List<SubjectCoverageDto>();
				foreach (var (categoryName, gradeMap) in timetal)
				{
					if (!gradeMap.TryGetValue(gradeLevel, out var vejledende) || vejledende <= 0)
					{
						continue;
					}

					if (!Enum.TryParse<SubjectCategory>(categoryName, out var category))
					{
						continue;
					}

					var actual = hoursPerCategory.GetValueOrDefault(category, 0.0);
					var status = actual == 0 ? "missing"
						: actual < vejledende * 0.75 ? "red"
						: actual < vejledende ? "yellow"
						: "green";

					subjects.Add(new SubjectCoverageDto(categoryName, Math.Round(actual, 2), status));
				}

				return new ClassCoverageDto(
					classGroup.Key.ClassId,
					classGroup.Key.Name,
					gradeLevel,
					subjects.OrderBy(s => s.Category).ToList());
			})
			.OrderBy(c => c.GradeLevel).ThenBy(c => c.ClassName)
			.ToList();

		// Also include classes with a grade but no active schema slots (all missing)
		var classesWithSlots = classes.Select(c => c.ClassId).ToHashSet();
		var allGradedClasses = await db.Classes
			.AsNoTracking()
			.Where(c => c.GradeLevel.HasValue && !classesWithSlots.Contains(c.Id))
			.ToListAsync(cancellationToken);

		foreach (var cls in allGradedClasses)
		{
			var gradeLevel = cls.GradeLevel!.Value;
			var subjects = new List<SubjectCoverageDto>();
			foreach (var (categoryName, gradeMap) in timetal)
			{
				if (!gradeMap.TryGetValue(gradeLevel, out var vejledende) || vejledende <= 0)
				{
					continue;
				}

				subjects.Add(new SubjectCoverageDto(categoryName, 0.0, "missing"));
			}

			if (subjects.Count > 0)
			{
				classes.Add(new ClassCoverageDto(cls.Id, cls.Name, gradeLevel, subjects.OrderBy(s => s.Category).ToList()));
			}

		}

		classes.Sort((a, b) =>
		{
			var g = a.GradeLevel.CompareTo(b.GradeLevel);
			return g != 0 ? g : string.Compare(a.ClassName, b.ClassName, StringComparison.Ordinal);
		});

		return Ok(new CoverageResponseDto(classes));
	}
}
