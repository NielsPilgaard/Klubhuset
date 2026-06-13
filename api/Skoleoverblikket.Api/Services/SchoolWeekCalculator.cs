using Skoleoverblikket.Api.Models;

namespace Skoleoverblikket.Api.Services;

public static class SchoolWeekCalculator
{
	/// <summary>
	/// Counts school weeks in [start, end], subtracting weeks where the full
	/// Mon–Fri is covered by CalendarEntries of type Ferie or Lukkedag.
	/// Returns 40 if start or end is null (safe fallback).
	/// </summary>
	public static int CountSchoolWeeks(DateOnly? start, DateOnly? end, IEnumerable<CalendarEntry> holidays)
	{
		if (start is null || end is null)
		{
			return 40;
		}

		var holidayList = holidays
			.Where(h => h.Type is CalendarEntryType.Ferie or CalendarEntryType.Lukkedag)
			.ToList();

		var count = 0;
		var monday = GetNextMonday(start.Value);

		while (monday <= end.Value)
		{
			var friday = monday.AddDays(4);
			if (!IsFullWeekHoliday(monday, friday, holidayList))
			{
				count++;
			}

			monday = monday.AddDays(7);
		}

		return count;
	}

	private static DateOnly GetNextMonday(DateOnly date)
	{
		var daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
		return date.AddDays(daysUntilMonday);
	}

	private static bool IsFullWeekHoliday(DateOnly monday, DateOnly friday, List<CalendarEntry> holidays)
	{
		// Check each school day Mon–Fri is covered by at least one holiday entry
		for (var day = monday; day <= friday; day = day.AddDays(1))
		{
			if (!holidays.Any(h => h.StartDate <= day && h.EndDate >= day))
			{
				return false;
			}
		}

		return true;
	}
}
