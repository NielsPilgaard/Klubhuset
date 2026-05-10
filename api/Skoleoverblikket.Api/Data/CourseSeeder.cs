using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.Data;

public static class CourseSeeder
{
	public static readonly (string Name, string Color, SubjectCategory Category)[] StandardCourses =
	[
		("Dansk",                "#3b82f6", SubjectCategory.Dansk),
		("Matematik",            "#f97316", SubjectCategory.Matematik),
		("Engelsk",              "#8b5cf6", SubjectCategory.Engelsk),
		("Naturfag",             "#10b981", SubjectCategory.Naturfag),
		("Historie",             "#f59e0b", SubjectCategory.Historie),
		("Musik",                "#ec4899", SubjectCategory.Musik),
		("Idræt",                "#06b6d4", SubjectCategory.Idraet),
		("Kristendomskundskab",  "#6366f1", SubjectCategory.Kristendomskundskab),
		("Billedkunst",          "#f43f5e", SubjectCategory.Billedkunst),
		("Håndværk og design",   "#84cc16", SubjectCategory.HaandvaerkOgDesign),
		("Tysk",                 "#14b8a6", SubjectCategory.Tysk),
		("Fransk",               "#eab308", SubjectCategory.Fransk),
		("Geografi",             "#a16207", SubjectCategory.Geografi),
		("Biologi",              "#16a34a", SubjectCategory.Biologi),
		("Fysik/kemi",           "#7c3aed", SubjectCategory.FysikKemi),
		("Samfundsfag",          "#dc2626", SubjectCategory.Samfundsfag),
	];

	public static IEnumerable<Course> BuildStandardCourses(Guid tenantId) =>
		StandardCourses.Select(c => new Course
		{
			Id = Guid.NewGuid(),
			TenantId = tenantId,
			Name = c.Name,
			Color = c.Color,
			Category = c.Category,
		});
}
